from pydantic import BaseModel, Field


class MicrobitExplainErrorRequest(BaseModel):
    error_message: str = Field(..., description="The error message to explain.")
    language: str = Field(default="vi", description="Language code for the response (e.g., 'vi' for Vietnamese, 'en' for English).")

class MicrobitExplainErrorResponse(BaseModel):
    explanation: str = Field(..., description="Child-friendly explanation of the error.")
    provider: str = Field(..., description="LLM provider that generated the response.")
    model: str = Field(..., description="LLM model identifier used for the response.")