
static IEnumerable<float> normalize(IEnumerable<float> x, float newLowerBound, float newUpperBound)
{
	float min = Enumerable.Min(x);
	float max = Enumerable.Max(x);
	float range = max - min;
	float newRange = newUpperBound - newLowerBound;
	
	return x.Select(a => ((a - min) / range) * newRange + newLowerBound);
}