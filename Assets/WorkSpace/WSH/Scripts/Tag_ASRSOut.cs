using UnityEngine;

namespace WSH
{
    public class Tag_ASRSOut : MonoBehaviour
    {
        public float watingTime;
        public float speed;
        public bool isOn;

        float timer = 0f;
        private void Update()
        {
            timer += Time.deltaTime * speed;
            isOn = timer >= watingTime;
        }
        AMR inAMR;
        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out AMR amr))
                return;
            Debug.Log($"On AMR : {other.name}");
            inAMR = amr;
        }
        private void OnTriggerStay(Collider other)
        {
            if (inAMR == null)
                return;
            if (!isOn)
                return;
            Debug.Log($"ASRS is Ready");
            timer = 0f;
            inAMR.isLoaded = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out AMR amr))
                return;
            Debug.Log($"Exit AMR : {inAMR.name}");
            inAMR = null;
        }
    }
}