
using FirstGearGames.Mirrors.Assets.FlexNetworkAnimators;
using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FNAS.Demos
{

    public class AutoMoveBlah : NetworkBehaviour
    {
        private FlexNetworkAnimator _fna;
        private NetworkAnimator _na;
        private bool _useFNA;
        private Animator _animator;
        public bool PlayLocally = true;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _fna = GetComponent<FlexNetworkAnimator>();
            _na = GetComponent<NetworkAnimator>();
            _useFNA = (_fna != null);
        }

        private void Update()
        {
            if (base.hasAuthority)
            {
                float horizontal = Input.GetAxis("Horizontal");
                float moveRate = 1f;
                transform.position += new Vector3(horizontal, 0f, 0f) * moveRate * Time.deltaTime;

                if (PlayLocally)
                    _animator.SetFloat("Horizontal", horizontal);
                CmdUpdateHorizontal(horizontal);

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    CmdJump();
                    if (PlayLocally)
                    {
                        if (_useFNA)
                            _fna.SetTrigger("Jump");
                        else
                            _na.SetTrigger("Jump");
                    }
                }
            }
        }

        [Command]
        private void CmdJump()
        {
            if (_useFNA)
                _fna.SetTrigger("Jump");
            else
                _na.SetTrigger("Jump");
        }

        [Command]
        private void CmdUpdateHorizontal(float horizontal)
        {
            _animator.SetFloat("Horizontal", horizontal);
        }


    }


}