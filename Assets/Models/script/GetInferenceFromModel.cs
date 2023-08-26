using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using System.Linq;
using Unity.Barracuda;

public class GetInferenceFromModel : MonoBehaviour
{
    public NNModel modelAsset;
    private Model _runtimeModel;
    private IWorker _engine; 
    public Texture2D texture; 

    [System.Serializable]
    public struct Prediction{
        public int predictedValue;
        public float[] predicted;

        public void SetPrediction(Tensor t){
            predicted = t.AsFloats();
            predictedValue = Array.IndexOf(predicted, predicted.Max());
            // Debug.Log("message" +  predictedValue);

        }
    }

    public Prediction prediction;
    // Start is called before the first frame update
    void Start()
    {
        _runtimeModel = ModelLoader.Load(modelAsset);
        _engine = WorkerFactory.CreateWorker(_runtimeModel, WorkerFactory.Device.GPU);
        prediction = new Prediction();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)){
            //making a tensor out of a grayscale texture
            var channelCount = 1; //1=grayscle, 3=color, 4=color+alpha
            var inputX = new Tensor(texture, channelCount);

            Tensor outputY = _engine.Execute(inputX).PeekOutput();
            inputX.Dispose();
            prediction.SetPrediction(outputY);
        }
        
    }

    private void OnDestory(){
        _engine.Dispose();
    }
}
