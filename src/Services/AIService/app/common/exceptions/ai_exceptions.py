class LLMResponseParseError(Exception):    
    def __init__(self, message: str, response_content: str | None = None):
        super().__init__(message)
        self.message = message
        self.response_content = response_content
    
    def __str__(self) -> str:
        if self.response_content:
            return f"{self.message}\nResponse content: {self.response_content[:200]}..."
        return self.message

