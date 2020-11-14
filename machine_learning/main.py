from fastapi import FastAPI
from pydantic import BaseModel
import pandas as pd
import json
import contact

class Request(BaseModel):
    s1: float = 643.33
    s2: float = 1590.17
    s3: float = 1404.7
    s4: float = 553.24
    s5: float = 2388.04
    s6: float = 9052.54
    s7: float = 47.52
    s8: float = 521.62
    s9: float = 2388.12
    s10: float = 8133.08
    s11: float = 8.4306
    s12: float = 392
    s13: float = 38.86
    s14: float = 23.3787


app = FastAPI()

@app.post('/request_tocontact')
async def contact_stakeholder(request: Request):
# to post any new job by title, alternateLabels, description and skill, and get the top 5 matches 

    s_recordings = [request.s1, request.s2, request.s3, request.s4, request.s5, request.s6, request.s7, request.s8, request.s9, request.s10, request.s11, request.s12, request.s13, request.s14]

    rul = contact.get_rul(model = contact.model, s_recordings = s_recordings)
    response = {"rul": rul[0] , "contact_stakeholder": contact.contact_stakeholder(rul[0])}
        
    return response

if __name__ == "__main__":
    # to run type in terminal 'uvicorn main:app' 
    uvicorn.run(app, host="0.0.0.0", port=8000)

   