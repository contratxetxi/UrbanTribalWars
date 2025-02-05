using System.Collections.Generic;

// Implementación sencilla de cola de prioridad para el algoritmo A*
public class PriorityQueue<T> {
    private List<KeyValuePair<T, int>> elements = new List<KeyValuePair<T, int>>();

    public int Count {
        get { return elements.Count; }
    }

    public void Enqueue(T item, int priority) {
        elements.Add(new KeyValuePair<T, int>(item, priority));
    }

    public T Dequeue() {
        int bestIndex = 0;
        for (int i = 0; i < elements.Count; i++) {
            if (elements[i].Value < elements[bestIndex].Value) {
                bestIndex = i;
            }
        }
        T bestItem = elements[bestIndex].Key;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}
