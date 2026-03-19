from app.core.rag.streaming_hooks import (
    HeavyHitterCounter,
    StreamingRAGRouter,
    MultiVectorCosineFilter,
)


def test_heavy_hitter_counter_tracks_topk():
    counter = HeavyHitterCounter(max_size=3)
    for item in ["a", "b", "a", "c", "d", "a"]:
        counter.add(item)
    top_items = counter.topk(2)
    assert "a" in top_items
    assert len(top_items) == 2


def test_streaming_router_flag():
    router = StreamingRAGRouter(freshness_feature_flag=True)
    assert router.route(has_fresh_chunks=True) == "streaming"
    assert router.route(has_fresh_chunks=False) == "batch"


def test_multivector_cosine_filter_threshold():
    filt = MultiVectorCosineFilter()
    vectors = [[1.0, 0.0], [0.0, 1.0]]
    query = [1.0, 0.0]

    indices = filt.filter(vectors=vectors, query_vector=query, threshold=0.9)
    # Only the first vector should be above threshold
    assert indices == [0]

