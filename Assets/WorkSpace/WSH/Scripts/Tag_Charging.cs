using UnityEngine;

namespace WSH
{
    public class Tag_Charging:MonoBehaviour
    {
        public float chargePerSecond;

        AMR inAMR;
        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out AMR amr))
                return;

            inAMR = amr;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.TryGetComponent(out AMR amr))
                return;
            if (inAMR == null)
                return;
            inAMR.batteryCapacity += chargePerSecond * Time.deltaTime;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out AMR amr))
                return;

            inAMR = null;
        }
    }
}