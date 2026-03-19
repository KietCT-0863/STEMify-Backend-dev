from typing import Dict, Any, Optional
import logging
import json
import base64
from pathlib import Path

from app.core.tools.base import Tool
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class VisionTool(Tool):
    """
    Vision Tool 
    
    Uses vision model (e.g., OpenAI vision API) to understand images.
    Provides detailed image understanding for 3D models.
    """

    def __init__(
        self,
        llm: Optional[LLMClient] = None,
        vision_service_url: Optional[str] = None,
    ):
        super().__init__(
            name="vision",
            description="Analyze images using vision model to understand 3D models, components, and visual elements. Provides detailed descriptions and understanding of image content.",
        )
        self.llm = llm
        self.vision_service_url = vision_service_url

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - analyze: Analyze image using vision model

        Parameters:
        - image_path: Path to image file
        - image_base64: Base64 encoded image data
        - prompt: Specific question or instruction about the image
        - model_type: Type of 3D model (optional)
        """
        action = parameters.get("action", "analyze")
        try:
            if action == "analyze":
                return await self._analyze_with_vision(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[VisionTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _analyze_with_vision(self, parameters: Dict[str, Any]) -> str:
        image_path = parameters.get("image_path")
        image_base64 = parameters.get("image_base64")
        prompt = parameters.get("prompt", "Describe this 3D model image in detail, including components, connections, and educational context.")
        model_type = parameters.get("model_type", "unknown")

        if not image_path and not image_base64:
            return json.dumps({"error": "Either image_path or image_base64 required"})

        # If LLM supports vision, use it
        if self.llm and hasattr(self.llm, "generate_with_vision"):
            try:
                # Prepare image data
                if image_path:
                    path = Path(image_path)
                    if not path.exists():
                        return json.dumps({"error": f"Image file not found: {image_path}"})
                    with open(path, "rb") as f:
                        image_data = base64.b64encode(f.read()).decode("utf-8")
                else:
                    image_data = image_base64

                # Call vision API
                response = await self.llm.generate_with_vision(
                    prompt=prompt,
                    image_data=image_data,
                )
                return json.dumps(
                    {
                        "description": response,
                        "model_type": model_type,
                        "method": "vision_model",
                    }
                )
            except Exception as e:
                logger.warning(f"Vision API not available, using fallback: {e}")

        # Fallback: Return basic description
        return json.dumps(
            {
                "description": f"This is a {model_type} 3D model image. Vision analysis requires vision-enabled LLM.",
                "model_type": model_type,
                "method": "fallback",
                "note": "Install vision-enabled LLM for detailed analysis",
            }
        )

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["analyze"],
                    "description": "Action to perform",
                    "default": "analyze",
                },
                "image_path": {
                    "type": "string",
                    "description": "Path to image file",
                },
                "image_base64": {
                    "type": "string",
                    "description": "Base64 encoded image data",
                },
                "prompt": {
                    "type": "string",
                    "description": "Specific question or instruction about the image",
                    "default": "Describe this 3D model image in detail",
                },
                "model_type": {
                    "type": "string",
                    "description": "Type of 3D model (e.g., microbit, arduino)",
                },
            },
            "required": ["action"],
        }

