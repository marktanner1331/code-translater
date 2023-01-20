import numpy as np

def normalize(x, newLowerBound, newUpperBound):
    min = np.min(x)
    max = np.max(x)
    range = max - min
    newRange = newUpperBound - newLowerBound

    return [((a - min) / range) * newRange + newLowerBound for a in x]