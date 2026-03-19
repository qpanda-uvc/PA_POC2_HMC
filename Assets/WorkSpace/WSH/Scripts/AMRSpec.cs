using UnityEditor;
using UnityEngine;

namespace WSH
{
    [CreateAssetMenu(fileName ="AMRSpecData", menuName ="Create AMRSpecData")]
    public class AMRSpec : ScriptableObject
    {
        public float moveSpeed;
        public float rotateSpeed;
        public float liftSpeed;
        public float dockingSpeed;
        public float bc_Move;
        public float bc_Idle;
        public float bc_Lift;
        public float bc_Docking;
        public float bc_Rotate;
    }
}