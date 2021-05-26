using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{


    public class PickableObjectClientAuth : NetworkBehaviour
    {
        /// <summary>
        /// Layers this object can attach to.
        /// </summary>
        public LayerMask AttachableLayers;
        /// <summary>
        /// True to attach to root. False to look for FlexAttachTarget.
        /// </summary>
        public bool AttachToRoot = false;
        /// <summary>
        /// FNT on this object.
        /// </summary>
        private FlexNetworkTransform _fnt;
        /// <summary>
        /// Target the ball should follow.
        /// </summary>
        private Transform _objectTarget = null;

        private void Awake()
        {
            _fnt = GetComponent<FlexNetworkTransform>();
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            _objectTarget = null;
        }

        private void LateUpdate()
        {
            /* IMPORTANT IMPORTANT 
             *  IMPORTANT IMPORTANT 
             *   IMPORTANT IMPORTANT */
            /* Notice I lock the ball onto the target in LateUpdate.
             * This is important because FNT processes smoothing in
             * update. If the ball were to move before smoothing is
             * run on FNT it may jitter on spectators. When using server
             * side Attached logic it's best to move with your target
             * in late update. This does not matter when using client authoritative
             * attached. */
            if (_objectTarget != null)
                transform.position = _objectTarget.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            //This example uses a server side ball.
            if (!NetworkServer.active)
                return;

            //Wrong layer.
            if (other.gameObject.layer != ToLayer(AttachableLayers.value))
                return;

            //Get the network identity on root.
            NetworkIdentity ni = other.transform.root.GetComponent<NetworkIdentity>();
            if (ni == null)
                return;

            FlexAttachTargets fct = other.transform.root.GetComponent<FlexAttachTargets>();
            if (!AttachToRoot && fct == null)
                return;


            //If already has an owner.
            if (ni.connectionToClient != null)
            {
                //Same owner.
                if (ni.connectionToClient == base.connectionToClient)
                    return;

                base.netIdentity.RemoveClientAuthority();
            }

            base.netIdentity.AssignClientAuthority(ni.connectionToClient);
            TargetControlOn(ni, fct.ReturnTargetIndex(other.gameObject));
        }

        [TargetRpc]
        private void TargetControlOn(NetworkIdentity ni, sbyte targetIndex)
        {
            //If using FlexAttachTargets.
            if (!AttachToRoot)
            {
                FlexAttachTargets fct = ni.GetComponent<FlexAttachTargets>();
                _objectTarget = fct.ReturnTarget(targetIndex).transform;
                _fnt.SetAttached(ni, targetIndex);
            }
            //Using root.
            else
            {
                _objectTarget = ni.transform;
                _fnt.SetAttached(ni);                
            }            
        }

        /// <summary>
        /// Converts a layer bitmask to int.
        /// </summary>
        /// <param name="bitmask"></param>
        /// <returns></returns>
        private int ToLayer(int bitmask)
        {
            int result = bitmask > 0 ? 0 : 31;
            while (bitmask > 1)
            {
                bitmask = bitmask >> 1;
                result++;
            }
            return result;
        }
    }

}