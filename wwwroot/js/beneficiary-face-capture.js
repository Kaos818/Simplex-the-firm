(() => {
  const video = document.getElementById("camera"), error = document.getElementById("error");
  const instruction = document.getElementById("instruction"), start = document.getElementById("startCapture");
  const host = document.getElementById("capture"), challenges = JSON.parse(host.dataset.challenges);
  let stream;
  navigator.mediaDevices.getUserMedia({video:{facingMode:"user",width:{ideal:640},height:{ideal:480}},audio:false})
    .then(s => { stream=s; video.srcObject=s; })
    .catch(() => error.textContent="Camera access was denied. Request manual verification for Director review.");
  start.addEventListener("click", async () => {
    if (!stream || start.disabled) return;
    start.disabled=true; error.textContent="";
    const canvas=document.createElement("canvas"); canvas.width=640; canvas.height=480;
    const frames=[], timestamps=[], stages=[];
    try {
      instruction.textContent="Look directly at the camera";
      for(let i=0;i<10;i++){
        await new Promise(resolve=>setTimeout(resolve,200));
        canvas.getContext("2d").drawImage(video,0,0,640,480);
        const blob=await new Promise(resolve=>canvas.toBlob(resolve,"image/jpeg",0.78));
        if(!blob) throw new Error("A camera frame could not be captured.");
        frames.push(blob); timestamps.push(Date.now()); stages.push(-1);
      }
      for(let stage=0;stage<challenges.length;stage++){
        instruction.textContent=({BLINK:"Blink naturally",TURN_LEFT:"Turn your head left",TURN_RIGHT:"Turn your head right",OPEN_MOUTH:"Open your mouth"})[challenges[stage]];
        for(let i=0;i<10;i++){
          await new Promise(resolve=>setTimeout(resolve,250));
          canvas.getContext("2d").drawImage(video,0,0,640,480);
          const blob=await new Promise(resolve=>canvas.toBlob(resolve,"image/jpeg",0.78));
          if(!blob) throw new Error("A camera frame could not be captured.");
          frames.push(blob); timestamps.push(Date.now()); stages.push(stage);
        }
      }
      instruction.textContent="Checking your capture…";
      const form=new FormData();
      form.append("sessionId",host.dataset.sessionId); form.append("timestamps",JSON.stringify(timestamps)); form.append("stageIndexes",JSON.stringify(stages));
      frames.forEach((frame,index)=>form.append("frames",frame,`frame-${index}.jpg`));
      const response=await fetch("/BeneficiaryPortal/SubmitFaceCapture",{method:"POST",body:form,headers:{"RequestVerificationToken":document.querySelector('input[name="__RequestVerificationToken"]').value}});
      const result=await response.json();
      if(!response.ok) throw new Error(result.message||"Verification could not be completed.");
      instruction.textContent=result.message;
      stream?.getTracks().forEach(track=>track.stop());
      if(result.canSubmit===true) document.getElementById("submitApplication")?.classList.remove("d-none");
    } catch(ex) { error.textContent=ex.message||"Verification could not be completed. Please try again."; start.disabled=false; }
  });
  window.addEventListener("beforeunload",()=>stream?.getTracks().forEach(track=>track.stop()));
})();
