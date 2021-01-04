using UnityEngine;

public interface IVectorGraphService
{
    void AddLine(string handPointReference, int i, Vector3 vectorPoint, Vector3 startPoint, Transform parent);
}