"""
Script to generate gRPC Python code from proto files
"""

import os
import subprocess
import sys
from pathlib import Path

# Get the project root directory (parent of scripts/)
PROJECT_ROOT = Path(__file__).parent.parent
PROTO_ROOT = PROJECT_ROOT.parent.parent / "BuildingBlocks" / "Shared" / "Protos"
OUTPUT_DIR = PROJECT_ROOT / "app" / "grpc" / "generated"

# Proto files to generate
CLASSROOM_PROTO_FILES = [
    "Classroom/classrooms.proto",
    "Classroom/classroom_student.proto",
    "Classroom/assignment_attempt.proto",
    "Classroom/assignment_question_attempt.proto",
    "Classroom/rubric_score.proto",
]

RESOURCE_PROTO_FILES = [
    "Resource/assignment.proto",
    "Resource/assignment_question.proto",
    "Resource/rubric_criterion.proto",
]


def ensure_output_dir(proto_package: str):
    """Ensure output directory exists."""
    output_path = OUTPUT_DIR / proto_package
    output_path.mkdir(parents=True, exist_ok=True)
    return output_path


def find_protoc():
    """Find protoc compiler."""
    # Try common locations
    protoc_candidates = [
        "protoc",
        "python -m grpc_tools.protoc",
    ]
    
    for protoc in protoc_candidates:
        try:
            if "python -m" in protoc:
                result = subprocess.run(
                    ["python", "-m", "grpc_tools.protoc", "--version"],
                    capture_output=True,
                    text=True,
                    check=True,
                )
            else:
                result = subprocess.run(
                    [protoc, "--version"],
                    capture_output=True,
                    text=True,
                    check=True,
                )
            print(f"Found protoc: {protoc}")
            return protoc
        except (subprocess.CalledProcessError, FileNotFoundError):
            continue
    
    raise RuntimeError(
        "protoc not found. Please install it:\n"
        "  - Install Protocol Buffers compiler: https://grpc.io/docs/protoc-installation/\n"
        "  - Or install grpcio-tools: pip install grpcio-tools"
    )


def generate_proto_files(proto_files: list[str], package_name: str):
    """Generate Python code from proto files."""
    output_dir = ensure_output_dir(package_name)
    
    # Find google/api and google/protobuf includes
    # These are typically in the protobuf installation
    import google.protobuf
    protobuf_path = Path(google.protobuf.__file__).parent.parent
    
    # Common include paths
    include_paths = [
        str(PROTO_ROOT.parent),  # BuildingBlocks/Shared level
        str(protobuf_path),  # google/protobuf
    ]
    
    # Try to find google/api annotations
    google_api_path = None
    for path in [
        protobuf_path.parent / "google" / "api",
        Path(sys.prefix) / "Lib" / "site-packages" / "google" / "api",
    ]:
        if path.exists():
            google_api_path = str(path.parent)
            break
    
    if google_api_path:
        include_paths.append(google_api_path)
    
    # Use python -m grpc_tools.protoc (recommended for Python projects)
    protoc_command = [
        sys.executable,
        "-m",
        "grpc_tools.protoc",
    ]
    
    # Add include paths
    for include_path in include_paths:
        protoc_command.extend(["-I", include_path])
    
    # Set output directory
    protoc_command.extend([
        f"--python_out={output_dir}",
        f"--grpc_python_out={output_dir}",
        f"--pyi_out={output_dir}",
    ])
    
    # Add proto files
    for proto_file in proto_files:
        proto_path = PROTO_ROOT / proto_file
        if not proto_path.exists():
            print(f"Warning: Proto file not found: {proto_path}")
            continue
        protoc_command.append(str(proto_path))
    
    print(f"Generating gRPC code for {package_name}...")
    print(f"Command: {' '.join(protoc_command)}")
    
    try:
        result = subprocess.run(
            protoc_command,
            check=True,
            capture_output=True,
            text=True,
        )
        print(f"Successfully generated gRPC code in {output_dir}")
        return True
    except subprocess.CalledProcessError as e:
        print(f"Error generating proto files:")
        print(f"stdout: {e.stdout}")
        print(f"stderr: {e.stderr}")
        return False


def create_init_file(package_name: str):
    """Create __init__.py file for the generated package."""
    init_file = OUTPUT_DIR / package_name / "__init__.py"
    if not init_file.exists():
        init_file.write_text('"""Generated gRPC code for {}."""\n'.format(package_name))
        print(f"Created {init_file}")


def copy_to_protos_unified(package_name: str):
    """Copy generated files to unified Protos directory for easier imports."""
    source_dir = OUTPUT_DIR / package_name / "Protos" / package_name
    target_dir = OUTPUT_DIR / "Protos" / package_name
    
    if not source_dir.exists():
        return
    
    import shutil
    target_dir.mkdir(parents=True, exist_ok=True)
    shutil.copytree(source_dir, target_dir, dirs_exist_ok=True)
    print(f"Copied {package_name} proto files to unified Protos directory")


def main():
    """Main function."""
    print("=" * 60)
    print("gRPC Code Generation Script")
    print("=" * 60)
    
    # Check if proto root exists
    if not PROTO_ROOT.exists():
        print(f"Error: Proto root directory not found: {PROTO_ROOT}")
        sys.exit(1)
    
    print(f"Proto root: {PROTO_ROOT}")
    print(f"Output directory: {OUTPUT_DIR}")
    
    # Generate Classroom proto files
    if CLASSROOM_PROTO_FILES:
        print("\n" + "=" * 60)
        print("Generating Classroom proto files...")
        print("=" * 60)
        
        success = generate_proto_files(CLASSROOM_PROTO_FILES, "Classroom")
        if success:
            create_init_file("Classroom")
            copy_to_protos_unified("Classroom")
            print("\n[OK] Classroom proto files generated successfully!")
        else:
            print("\n[ERROR] Failed to generate Classroom proto files")
            sys.exit(1)
    
    if RESOURCE_PROTO_FILES:
        print("\n" + "=" * 60)
        print("Generating Resource proto files...")
        print("=" * 60)
        
        success = generate_proto_files(RESOURCE_PROTO_FILES, "Resource")
        if success:
            create_init_file("Resource")
            copy_to_protos_unified("Resource")
            print("\n[OK] Resource proto files generated successfully!")
        else:
            print("\n[ERROR] Failed to generate Resource proto files")
            sys.exit(1)
    
    print("\n" + "=" * 60)
    print("Generation complete!")
    print("=" * 60)


if __name__ == "__main__":
    main()
