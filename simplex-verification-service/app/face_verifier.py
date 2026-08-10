import cv2
import numpy as np
from .config import settings
from .face_detector import detections, detector_ready, faces
from .face_matcher import align, compare, matcher_ready
from .image_validation import decode_image
from .liveness_detector import evaluate_challenge, validate_challenges

def result(session, decision, code, reason, valid=0.0, duplicate=0.0, live=False, matched=False, score=None, challenges=None):
    return {"session_id": session, "decision": decision, "liveness_passed": live, "face_matched": matched,
            "similarity_score": score, "valid_frame_ratio": valid, "duplicate_frame_ratio": duplicate,
            "challenge_results": challenges or [], "reason_code": code, "reason": reason}

def verify(reference_bytes, frame_bytes, payload):
    session=str(payload.get("session_id","")); expected=payload.get("challenges",[]); stages=payload.get("stage_indexes",[]); times=payload.get("timestamps",[])
    if not validate_challenges(expected, expected) or len(frame_bytes) not in range(20,61) or len(stages)!=len(frame_bytes) or len(times)!=len(frame_bytes):
        return result(session,"INVALID_CAPTURE","CAPTURE_METADATA_INVALID","The camera capture was incomplete.")
    if any(x not in (-1, 0, 1, 2) for x in stages) or any(times[i]>=times[i+1] for i in range(len(times)-1)):
        return result(session,"INVALID_CAPTURE","CAPTURE_METADATA_INVALID","The camera capture was incomplete.")
    if not detector_ready() or not matcher_ready():
        return result(session,"MANUAL_REVIEW","LOCAL_MODEL_UNAVAILABLE","Automatic face matching is unavailable and requires administrator review.")
    reference=decode_image(reference_bytes)
    reference_detections=detections(reference)
    if reference is None or len(reference_detections)!=1:
        return result(session,"MANUAL_REVIEW","REFERENCE_FACE_UNCERTAIN","The identity-document photograph needs administrator review.")
    decoded=[]; boxes=[]; face_detections=[]; hashes=[]; decoded_stages=[]
    for index,raw in enumerate(frame_bytes):
        image=decode_image(raw)
        if image is None: continue
        found=detections(image)
        if len(found)>1: return result(session,"INVALID_CAPTURE","MULTIPLE_FACES","Only one person may appear in the camera.")
        if len(found)==1:
            box=tuple(map(int, found[0][:4]))
            x,y,w,h=box
            if w < 60 or h < 60 or x < 0 or y < 0 or x+w > image.shape[1] or y+h > image.shape[0]:
                continue
            decoded.append(image); boxes.append(box); face_detections.append(found[0]); decoded_stages.append(stages[index])
            hashes.append(cv2.resize(cv2.cvtColor(image,cv2.COLOR_BGR2GRAY),(16,16)).flatten())
    valid=len(decoded)/len(frame_bytes)
    if valid<0.65: return result(session,"INVALID_CAPTURE","FACE_NOT_VISIBLE","Keep your face clearly visible throughout the verification.",valid)
    duplicate=sum(np.linalg.norm(hashes[i].astype(float)-hashes[i-1].astype(float))<50 for i in range(1,len(hashes)))/max(1,len(hashes)-1)
    if duplicate>0.35: return result(session,"FAILED_LIVENESS","STATIC_OR_DUPLICATE_FRAMES","The capture appeared static. Please complete the live verification again.",valid,duplicate)
    centers=np.array([[x+w/2,y+h/2] for x,y,w,h in boxes],dtype=float)
    widths=np.array([max(1,w) for _,_,w,_ in boxes],dtype=float)
    jumps=np.linalg.norm(np.diff(centers,axis=0),axis=1)/np.maximum(1.0,widths[:-1])
    if len(jumps) and float(np.percentile(jumps,90))>0.75:
        return result(session,"FAILED_LIVENESS","DISCONTINUOUS_FACE_MOVEMENT","The capture was discontinuous. Please complete the live verification again.",valid,duplicate)
    checks=[]
    for stage,challenge in enumerate(expected):
        indexes=[i for i in range(len(boxes)) if decoded_stages[i]==stage]
        checks.append(evaluate_challenge(challenge,[decoded[i] for i in indexes],[boxes[i] for i in indexes]))
    if not all(x["passed"] for x in checks):
        return result(session,"FAILED_LIVENESS","LIVENESS_CHALLENGE_FAILED","One or more live actions could not be confirmed. Please complete the live verification again.",valid,duplicate,challenges=checks)
    reference_crop=align(reference,reference_detections[0])
    best=max(range(len(decoded)),key=lambda i:boxes[i][2]*boxes[i][3]); x,y,w,h=boxes[best]
    live_crop=align(decoded[best],face_detections[best])
    if reference_crop is None or live_crop is None:
        return result(session,"MANUAL_REVIEW","FACE_ALIGNMENT_FAILED","The capture needs administrator review.",valid,duplicate,True,False,None,checks)
    score=compare(reference_crop,live_crop)
    if score<=0: return result(session,"MANUAL_REVIEW","LOCAL_MODEL_UNAVAILABLE","Your face needs administrator review.",valid,duplicate,True,False,None,checks)
    if score>=settings.face_match: return result(session,"VERIFIED",None,None,valid,duplicate,True,True,score,checks)
    if score>=settings.manual_review: return result(session,"MANUAL_REVIEW","MATCH_UNCERTAIN","Your face could not be verified confidently.",valid,duplicate,True,False,score,checks)
    return result(session,"FACE_NOT_MATCHED","FACE_NOT_MATCHED","Your face could not be matched to the identity document.",valid,duplicate,True,False,score,checks)
