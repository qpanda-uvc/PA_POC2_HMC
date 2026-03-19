import google.generativeai as genai
import os
from dotenv import load_dotenv

# .env 파일에서 API 키 로드
load_dotenv('key.env')
genai.configure(api_key=os.getenv("GOOGLE_API_KEY"))

print("사용 가능한 모델 목록:")
for m in genai.list_models():
    if 'generateContent' in m.supported_generation_methods:
        print(f"- Name: {m.name}")