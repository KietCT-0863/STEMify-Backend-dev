"""
Example usage of Graph Reasoning Engine
"""

import asyncio
import json
from app.core.reasoning import create_reasoning_orchestrator


async def main():
    """Example: Use reasoning engine to answer teacher questions"""
    
    # Create orchestrator
    print("Creating reasoning orchestrator...")
    orchestrator = create_reasoning_orchestrator()
    
    # Example questions
    questions = [
        "Which students are struggling with electrical circuits?",
        "Show me students who need extra help in classroom 7A",
        "What topics are performing poorly?",
        "How has student performance changed in the last month?"
    ]
    
    for question in questions:
        print(f"\n{'='*60}")
        print(f"Question: {question}")
        print('='*60)
        
        try:
            # Execute reasoning
            result = await orchestrator.reason(question)
            
            # Display results
            print(f"\nPlan: {result.plan}")
            print(f"\nAnswer:\n{result.answer_teacher_friendly}")
            print(f"\nNext Actions:")
            for action in result.next_actions:
                print(f"  - {action}")
            
            print(f"\nCausal Findings: {len(result.causal_findings)}")
            for finding in result.causal_findings[:3]:
                print(f"  - {finding.hypothesis} (confidence: {finding.confidence:.0%})")
            
            print(f"\nEvidence: {len(result.evidence_pack.graph_refs)} graph refs, "
                  f"{len(result.evidence_pack.paths)} paths, "
                  f"{len(result.evidence_pack.texts)} texts")
            
            print(f"\nAudit: {result.audit}")
            
            # Export to JSON
            result_dict = result.to_dict()
            print(f"\nJSON Output (first 500 chars):")
            print(json.dumps(result_dict, indent=2)[:500] + "...")
            
        except Exception as e:
            print(f"Error: {e}")
            import traceback
            traceback.print_exc()


if __name__ == "__main__":
    asyncio.run(main())













