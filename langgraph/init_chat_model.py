from dotenv import load_dotenv
from langchain.chat_models import init_chat_model

# .env 파일에서 환경 변수 로드
load_dotenv()

# Anthropic Claude 설정
claude = init_chat_model("anthropic:claude-sonnet-4-6")

# Google Gemini 설정
gemini = init_chat_model("google-genai:gemini-2.5-flash")

# OpenAI GPT (비교를 위해)
gpt = init_chat_model("openai:gpt-4.1-mini")

# 동일한 인터페이스로 사용
#claude_response = claude.invoke("Explain the concept of Directed Graph.")
#print("Claude's response:", claude_response.content)

gemini_response = gemini.invoke("Explain the components of LangGraph.")
print("Gemini's response:", gemini_response.content)
