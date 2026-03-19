"""
Unit tests for Query Complexity Analyzer
"""

import pytest
from app.core.query.complexity_analyzer import QueryComplexityAnalyzer, ComplexityClassification


class TestQueryComplexityAnalyzer:
    """Test Query Complexity Analyzer"""
    
    def setup_method(self):
        """Setup test fixtures"""
        self.analyzer = QueryComplexityAnalyzer()
    
    def test_simple_query_what_is(self):
        """Test simple 'what is' query"""
        query = "What is the average score for student 123?"
        result = self.analyzer.analyze(query)
        
        assert result.classification == ComplexityClassification.SIMPLE
        assert result.score < 0.5
        assert "Simple patterns" in result.reasoning or result.score < 0.4
    
    def test_simple_query_show_me(self):
        """Test simple 'show me' query"""
        query = "Show me students in classroom 7A"
        result = self.analyzer.analyze(query)
        
        assert result.classification == ComplexityClassification.SIMPLE
        assert result.score < 0.5
    
    def test_complex_query_why(self):
        """Test complex 'why' query"""
        query = "Why does student Tuấn struggle with electrical circuits?"
        result = self.analyzer.analyze(query)
        
        assert result.classification == ComplexityClassification.COMPLEX
        assert result.score > 0.5
        assert "Complex patterns" in result.reasoning or result.score > 0.6
    
    def test_complex_query_compare(self):
        """Test complex 'compare' query"""
        query = "Compare the performance of students in classroom 7A and 7B"
        result = self.analyzer.analyze(query)
        
        assert result.classification == ComplexityClassification.COMPLEX
        assert result.score > 0.5
    
    def test_complex_query_how(self):
        """Test complex 'how' query"""
        query = "How can I help students who are struggling with physics topics?"
        result = self.analyzer.analyze(query)
        
        assert result.classification == ComplexityClassification.COMPLEX
        assert result.score > 0.5
    
    def test_short_query(self):
        """Test short query (likely simple)"""
        query = "Student 123"
        result = self.analyzer.analyze(query)
        
        # Short queries are more likely simple
        assert result.factors["word_count"] < 0.5
    
    def test_long_query(self):
        """Test long query (likely complex)"""
        query = "Can you please explain in detail why student Tuấn is performing poorly in electrical circuits and what specific topics he struggles with the most and how we can help him improve his understanding of these concepts?"
        result = self.analyzer.analyze(query)
        
        # Long queries are more likely complex
        assert result.factors["word_count"] > 0.5
    
    def test_multiple_entities(self):
        """Test query with multiple entities (likely complex)"""
        query = "Compare student Tuấn and student Lan performance in physics"
        result = self.analyzer.analyze(query)
        
        assert result.factors["entity_count"] > 0.3
    
    def test_is_simple_method(self):
        """Test is_simple() convenience method"""
        simple_query = "What is the score for student 123?"
        complex_query = "Why does student 123 struggle with physics?"
        
        assert self.analyzer.is_simple(simple_query) == True
        assert self.analyzer.is_simple(complex_query) == False
    
    def test_is_complex_method(self):
        """Test is_complex() convenience method"""
        simple_query = "What is the score for student 123?"
        complex_query = "Why does student 123 struggle with physics?"
        
        assert self.analyzer.is_complex(simple_query) == False
        assert self.analyzer.is_complex(complex_query) == True
    
    def test_empty_query(self):
        """Test empty query"""
        result = self.analyzer.analyze("")
        
        assert result.classification == ComplexityClassification.UNKNOWN
        assert result.score == 0.5
    
    def test_factors_present(self):
        """Test that all factors are present in result"""
        query = "Why does student 123 struggle with physics?"
        result = self.analyzer.analyze(query)
        
        assert "word_count" in result.factors
        assert "simple_patterns" in result.factors
        assert "complex_patterns" in result.factors
        assert "entity_count" in result.factors
        assert "question_type" in result.factors






