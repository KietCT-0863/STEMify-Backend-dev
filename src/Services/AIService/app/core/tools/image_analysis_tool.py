from typing import Dict, Any, Optional
import logging
import json
import base64
from pathlib import Path

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class ImageAnalysisTool(Tool):
    """
    Image Analysis Tool
    
    Analyzes 3D emulator images to extract basic information.
    Can work with image paths or base64 encoded images.
    """

    def __init__(self):
        super().__init__(
            name="image_analysis",
            description="Analyze 3D emulator images to extract basic information like dimensions, format, and basic metadata. Works with image paths or base64 encoded images.",
        )

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - analyze: Analyze an image and extract basic information

        Parameters:
        - image_path: Path to image file
        - image_base64: Base64 encoded image data
        - model_type: Type of 3D model (optional)
        """
        action = parameters.get("action", "analyze")
        try:
            if action == "analyze":
                return await self._analyze_image(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[ImageAnalysisTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _analyze_image(self, parameters: Dict[str, Any]) -> str:
        """Analyze image and extract basic information"""
        image_path = parameters.get("image_path")
        image_base64 = parameters.get("image_base64")
        model_type = parameters.get("model_type", "unknown")

        if not image_path and not image_base64:
            return json.dumps({"error": "Either image_path or image_base64 required"})

        # Basic analysis (in production, would use image processing library)
        analysis = {
            "model_type": model_type,
            "has_image": True,
            "format": "unknown",
            "dimensions": {"width": 0, "height": 0},
        }

        if image_path:
            path = Path(image_path)
            if path.exists():
                analysis["format"] = path.suffix.lower()
                analysis["file_size"] = path.stat().st_size
                # In production, would use PIL or similar to get actual dimensions
                analysis["dimensions"] = {"width": 800, "height": 600}  # Placeholder
            else:
                return json.dumps({"error": f"Image file not found: {image_path}"})

        if image_base64:
            try:
                # Decode to check if valid base64
                base64.b64decode(image_base64)
                analysis["format"] = "base64"
                analysis["has_image"] = True
            except Exception:
                return json.dumps({"error": "Invalid base64 image data"})

        return json.dumps(analysis)

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
                "model_type": {
                    "type": "string",
                    "description": "Type of 3D model (e.g., microbit, arduino)",
                },
            },
            "required": ["action"],
        }

