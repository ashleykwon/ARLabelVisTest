using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreeDimensionScreenCameraSync : MonoBehaviour
{
    [SerializeField] private Transform _centerEyeAnchor;

    private Vector3 _originPos;
    
    void Start()
    {
        _originPos = transform.position;
    }

    void Update()
    {
        transform.position = _originPos + _centerEyeAnchor.localPosition;
    }
}
