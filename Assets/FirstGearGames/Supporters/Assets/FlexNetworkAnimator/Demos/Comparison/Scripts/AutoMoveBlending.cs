
using FirstGearGames.Mirrors.Assets.FlexNetworkAnimators;
using Mirror;
using System.Collections;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FNAS.Demos
{

    public class AutoMoveBlending : NetworkBehaviour
    {
        private Animator _animator;
        private float _center;


        public override void OnStartServer()
        {
            base.OnStartServer();
            _animator = GetComponent<Animator>();
            _center = transform.position.x;

            StartCoroutine(__Move());
        }

        private IEnumerator __Move()
        {
            WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
            float right = _center + 2f;
            float left = _center - 2f;
            bool goRight = true;
            float horizontal = 0f;
            float moveRate = 2f;
            while (true)
            {
                float xGoal = (goRight) ? right : left;
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(xGoal, transform.position.y, transform.position.z), moveRate * Time.deltaTime);
                horizontal = Mathf.MoveTowards(horizontal, Mathf.Sign(xGoal), moveRate * Time.deltaTime);
                _animator.SetFloat("Horizontal", horizontal);
                if (transform.position.x == xGoal)
                    goRight = !goRight;

                yield return endOfFrame;
            }
        }



    }


}