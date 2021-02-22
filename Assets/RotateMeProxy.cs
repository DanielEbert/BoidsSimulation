using Unity.Entities;
using Unity.Transforms;
using UnityEngine;


public struct RotateMe : IComponentData
{ }

public class RotateMeProxy : ComponentDataProxy<RotateMe> { }