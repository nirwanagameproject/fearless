using FirstGearGames.Utilities.Networks;
#if MIRAGE
using Mirage;
#else
using Mirror;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkAnimators
{
    public class FlexNetworkAnimator : NetworkBehaviour
    {
        #region Types.
        public class ClientAuthoritativeUpdate
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="forceAllRequirement">Maximum size Buffers can reach before entries are dumped and a forceAll must be used.</param>
            public ClientAuthoritativeUpdate()
            {
                //Start buffers off at 8 bytes nad grow them as needed.
                for (int i = 0; i < MAXIMUM_BUFFER_COUNT; i++)
                    _buffers.Add(new byte[DEFAULT_BUFFER_SIZE]);

                _bufferLengths = new int[MAXIMUM_BUFFER_COUNT];
            }

            #region Public.
            /// <summary>
            /// True to force all animator data and ignore buffers.
            /// </summary>
            public bool ForceAll { get; private set; }
            /// <summary>
            /// Number of entries in Buffers.
            /// </summary>
            public int BufferCount = 0;
            #endregion

            #region Private.
            /// <summary>
            /// Length of buffers.
            /// </summary>
            private int[] _bufferLengths;
            /// <summary>
            /// Buffers.
            /// </summary>
            private List<byte[]> _buffers = new List<byte[]>();
            #endregion

            #region Const.
            /// <summary>
            /// Maximum size data may be.
            /// </summary>
            private const int MAXIMUM_DATA_SIZE = 1000;
            /// <summary>
            /// Maximum number of allowed buffers.
            /// </summary>
            private const int MAXIMUM_BUFFER_COUNT = 2;
            /// <summary>
            /// Default size for buffers.
            /// </summary>
            private const int DEFAULT_BUFFER_SIZE = 64;
            #endregion

            public void AddToBuffer(ref ArraySegment<byte> data)
            {
                int dataCount = data.Count;
                /* Data will never get this large, it's quite impossible.
                 * Just ignore the data if it does, client is likely performing
                 * an attack. */
                if (dataCount > MAXIMUM_DATA_SIZE)
                    return;

                //If index exceeds buffer count.
                if (BufferCount >= MAXIMUM_BUFFER_COUNT)
                {
                    ForceAll = true;
                    return;
                }

                /* If here, can write to buffer. */
                byte[] buffer = _buffers[BufferCount];

                //Make sure buffer is long enough.
                if (buffer.Length < dataCount)
                {
                    //Make next size fit and add on a little extra to try and reduce resize changes.
                    int nextSize = buffer.Length + dataCount + DEFAULT_BUFFER_SIZE;
                    if (nextSize > MAXIMUM_DATA_SIZE)
                        nextSize = MAXIMUM_DATA_SIZE;

                    Array.Resize(ref buffer, nextSize);
                }

                Buffer.BlockCopy(data.Array, data.Offset, buffer, 0, dataCount);
                _bufferLengths[BufferCount] = dataCount;
                BufferCount++;
            }

            /// <summary>
            /// Sets referenced data to buffer and it's length for index.
            /// </summary>
            /// <param name="index"></param>
            /// <param name="buffer"></param>
            /// <param name="length"></param>
            public void SetBuffer(int index, ref byte[] buffer, ref int length)
            {
                //Not going to check index because this is all internal and there's no way to exceed lengths.
                buffer = _buffers[index];
                length = _bufferLengths[index];
            }
            /// <summary>
            /// Resets buffers.
            /// </summary>
            public void Reset()
            {
                BufferCount = 0;
                ForceAll = false;
            }

        }
        /// <summary>
        /// Information on how to smooth to a float value.
        /// </summary>
        private struct SmoothedFloat
        {
            public SmoothedFloat(float rate, float target)
            {
                Rate = rate;
                Target = target;
            }

            public readonly float Rate;
            public readonly float Target;
        }

        private struct TriggerUpdate
        {
            public byte ParameterIndex;
            public bool Setting;

            public TriggerUpdate(byte parameterIndex, bool setting)
            {
                ParameterIndex = parameterIndex;
                Setting = setting;
            }
        }
        /// <summary>
        /// Details about an animator parameter.
        /// </summary>
        private class ParameterDetail
        {
            /// <summary>
            /// Parameter information.
            /// </summary>
            public readonly AnimatorControllerParameter ControllerParameter = null;
            /// <summary>
            /// Index within the types collection for this parameters value. The exception is with triggers; if the parameter type is a trigger then a value of 1 is set, 0 is unset.
            /// </summary>
            public readonly byte TypeIndex = 0;
            /// <summary>
            /// Hash for the animator string.
            /// </summary>
            public readonly int Hash;

            public ParameterDetail(AnimatorControllerParameter controllerParameter, byte typeIndex)
            {
                ControllerParameter = controllerParameter;
                TypeIndex = typeIndex;
                Hash = controllerParameter.nameHash;
            }
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// The animator component to synchronize.
        /// </summary>
        [Tooltip("The animator component to synchronize.")]
        [SerializeField]
        private Animator _animator;
        /// <summary>
        /// The animator component to synchronize.
        /// </summary>
        public Animator Animator { get { return _animator; } }
        /// <summary>
        /// True to smooth float value changes for spectators.
        /// </summary>
        [Tooltip("True to smooth float value changes for spectators.")]
        [SerializeField]
        private bool _smoothFloats = true;
        /// <summary>
        /// How much time to fall behind when using smoothing. Only increase value if the smoothing is sometimes jittery. Recommended values are between 0 and 0.04.
        /// </summary>
        [Tooltip("How much time to fall behind when using smoothing. Only increase value if the smoothing is sometimes jittery. Recommended values are between 0 and 0.04.")]
        [Range(0f, 0.1f)]
        [SerializeField]
        private float _interpolationFallbehind = 0.02f;
        /// <summary>
        /// How often to synchronize this animator.
        /// </summary>
        [Tooltip("How often to synchronize this animator.")]
        [Range(0.01f, 0.5f)]
        [SerializeField]
        private float _synchronizeInterval = 0.1f;
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True if using client authoritative animations.")]
        [SerializeField]
        private bool _clientAuthoritative = true;
        /// <summary>
        /// True if using client authoritative animations.
        /// </summary>
        public bool ClientAuthoritative { get { return _clientAuthoritative; } }
        /// <summary>
        /// True to synchronize server results back to owner. Typically used when you are changing animations on the server and are relying on the server response to update the clients animations.
        /// </summary>
        [Tooltip("True to synchronize server results back to owner. Typically used when you are changing animations on the server and are relying on the server response to update the clients animations.")]
        [SerializeField]
        private bool _synchronizeToOwner = false;
        #endregion

        #region Private.
        /// <summary>
        /// All parameter values, excluding triggers.
        /// </summary>
        private List<ParameterDetail> _parameterDetails = new List<ParameterDetail>();
        /// <summary>
        /// Last int values.
        /// </summary>
        private List<int> _ints = new List<int>();
        /// <summary>
        /// Last float values.
        /// </summary>
        private List<float> _floats = new List<float>();
        /// <summary>
        /// Last bool values.
        /// </summary>
        private List<bool> _bools = new List<bool>();
        /// <summary>
        /// Last layer weights.
        /// </summary>
        private float[] _layerWeights = null;
        /// <summary>
        /// Last speed.
        /// </summary>
        private float _speed = 0f;
        /// <summary>
        /// Next time client may send parameter updates.
        /// </summary>
        private float _nextClientSendTime = -1f;
        /// <summary>
        /// Next time server may send parameter updates.
        /// </summary>
        private float _nextServerSendTime = -1f;
        /// <summary>
        /// Trigger values set by using SetTrigger and ResetTrigger.
        /// </summary>
        private List<TriggerUpdate> _triggerUpdates = new List<TriggerUpdate>();
        /// <summary>
        /// Returns if the animator is exist and is active.
        /// </summary>
        private bool _isActive
        {
            get { return (_animator != null && _animator.enabled); }
        }
        /// <summary>
        /// Float valeus to smooth towards.
        /// </summary>
        private Dictionary<int, SmoothedFloat> _smoothedFloats = new Dictionary<int, SmoothedFloat>();
        /// <summary>
        /// Returns if floats can be smoothed for this client.
        /// </summary>
        private bool _canSmoothFloats
        {
            get
            {
                //Don't smooth on server only.
                if (!this.ReturnIsClient())
                    return false;
                //Smoothing is disabled.
                if (!_smoothFloats)
                    return false;
                //No reason to smooth for self.
                if (this.ReturnHasAuthority() && ClientAuthoritative)
                    return false;

                //Fall through.
                return true;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private byte? _cachedComponentIndex = null;
        /// <summary>
        /// Cached ComponentIndex for the NetworkBehaviour this FNA is on. This is because Mirror codes bad.
        /// </summary>
        public byte CachedComponentIndex
        {
            get
            {
                if (_cachedComponentIndex == null)
                {
                    //Exceeds value.
                    if (base.ComponentIndex > 255)
                    {
                        Debug.LogError("ComponentIndex is larger than supported type.");
                        _cachedComponentIndex = 0;
                    }
                    //Doesn't exceed value.
                    else
                    {
                        _cachedComponentIndex = (byte)Mathf.Abs(base.ComponentIndex);
                    }
                }

                return _cachedComponentIndex.Value;
            }
        }
        /// <summary>
        /// NetworkVisibility component on the root of this object.
        /// </summary>
        private NetworkVisibility _networkVisibility = null;
        /// <summary>
        /// Layers which need to have their state synchronized. Byte is the ParameterIndex.
        /// </summary>
        private HashSet<int> _unsynchronizedLayerStates = new HashSet<int>();
        /// <summary>
        /// Last animator set.
        /// </summary>
        private Animator _lastAnimator = null;
        /// <summary>
        /// Last Controller set.
        /// </summary>
        private RuntimeAnimatorController _lastController = null;
        /// <summary>
        /// NetworkWriter for this animator. Must use this instead of the pool because the pool returns the writer too early which corrupts array segments.
        /// </summary>
        private NetworkWriter _writer = new NetworkWriter();
        /// <summary>
        /// Holds client authoritative updates received to send to other clients.
        /// </summary>
        private ClientAuthoritativeUpdate _clientAuthoritativeUpdates;
        #endregion

        #region Const.
        /// <summary>
        /// ParameterDetails index which indicates a layer weight change.
        /// </summary>
        private const byte LAYER_WEIGHT = 240;
        /// <summary>
        /// ParameterDetails index which indicates an animator speed change.
        /// </summary>
        private const byte SPEED = 241;
        /// <summary>
        /// ParameterDetails index which indicates a layer state change.
        /// </summary>
        private const byte STATE = 242;
        #endregion

        private void Awake()
        {
            Initialize();
#if MIRAGE
            base.NetIdentity.OnStartServer.AddListener(StartServer);
#endif
        }

        protected virtual void OnDestroy()
        {
#if MIRAGE
            base.NetIdentity.OnStopServer.RemoveListener(StartServer);
#endif            
        }

        #region OnSerialize/Deserialize.
        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                bool updated = AnimatorUpdated(out ArraySegment<byte> updatedBytes, true);
                writer.WriteBoolean(updated);
                if (updated)
                    writer.WriteBytesAndSizeSegment(updatedBytes);
            }

            return base.OnSerialize(writer, initialState);
        }
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                bool updated = reader.ReadBoolean();
                if (updated)
                {
                    ArraySegment<byte> ps = reader.ReadBytesAndSizeSegment();
                    ApplyParametersUpdated(ref ps);
                }
            }

            base.OnDeserialize(reader, initialState);
        }
        #endregion

#if MIRROR
        public override void OnStartServer()
        {
            base.OnStartServer();
            StartServer();
        }

#endif
        private void StartServer()
        {
            //If using client authoritative then initialize clientAuthoritativeUpdates.
            if (_clientAuthoritative)
                _clientAuthoritativeUpdates = new ClientAuthoritativeUpdate();

            _networkVisibility = transform.root.GetComponent<NetworkVisibility>();
        }

        protected virtual void OnEnable()
        {
            FlexNetworkAnimatorManager.AddToActive(this);
        }
        protected virtual void OnDisable()
        {
            FlexNetworkAnimatorManager.RemoveFromActive(this);
        }

        public void ManualUpdate()
        {
            if (this.ReturnIsClient())
            {
                CheckSendToServer();
                SmoothFloats();
            }
            if (this.ReturnIsServer())
            {
                CheckSendToClients();
            }
        }

        protected virtual void Reset()
        {
            if (_animator == null)
                SetAnimator(GetComponent<Animator>());
        }

        /// <summary>
        /// Initializes this script for use. Should only be completed once.
        /// </summary>
        private void Initialize()
        {
            if (!_isActive)
            {
                Debug.LogWarning("Animator is null or not enabled; unable to initialize for animator. Use SetAnimator if animator was changed or enable the animator.");
                return;
            }

            //Speed.
            _speed = _animator.speed;

            //Build layer weights.
            _layerWeights = new float[_animator.layerCount];
            for (int i = 0; i < _layerWeights.Length; i++)
                _layerWeights[i] = _animator.GetLayerWeight(i);

            _parameterDetails.Clear();
            _bools.Clear();
            _floats.Clear();
            _ints.Clear();
            //Create a parameter detail for each parameter that can be synchronized.
            foreach (AnimatorControllerParameter item in _animator.parameters)
            {
                if (!_animator.IsParameterControlledByCurve(item.name))
                {
                    //Over 250 parameters; who would do this!?
                    if (_parameterDetails.Count == 240)
                    {
                        Debug.LogError("Parameter " + item.name + " exceeds the allowed 250 parameter count and is being ignored.");
                        continue;
                    }

                    int typeIndex = 0;
                    //Bools.
                    if (item.type == AnimatorControllerParameterType.Bool)
                    {
                        typeIndex = _bools.Count;
                        _bools.Add(_animator.GetBool(item.nameHash));
                    }
                    //Floats.
                    else if (item.type == AnimatorControllerParameterType.Float)
                    {
                        typeIndex = _floats.Count;
                        _floats.Add(_animator.GetFloat(item.name));
                    }
                    //Ints.
                    else if (item.type == AnimatorControllerParameterType.Int)
                    {
                        typeIndex = _ints.Count;
                        _ints.Add(_animator.GetInteger(item.nameHash));
                    }
                    //Triggers.
                    else if (item.type == AnimatorControllerParameterType.Trigger)
                    {
                        /* Triggers aren't persistent so they don't use stored values
                         * but I do need to make a parameter detail to track the hash. */
                        typeIndex = -1;
                    }

                    _parameterDetails.Add(new ParameterDetail(item, (byte)typeIndex));
                }
            }
        }

        /// <summary>
        /// Sets which animator to use. You must call this with the appropriate animator on all clients and server. This change is not automatically synchronized.
        /// </summary>
        /// <param name="animator"></param>
        public void SetAnimator(Animator animator)
        {
            //No update required.
            if (animator == _lastAnimator)
                return;

            _animator = animator;
            Initialize();
            _lastAnimator = animator;
        }

        /// <summary>
        /// Sets which controller to use. You must call this with the appropriate controller on all clients and server. This change is not automatically synchronized.
        /// </summary>
        /// <param name="controller"></param>        
        public void SetController(RuntimeAnimatorController controller)
        {
            //No update required.
            if (controller == _lastController)
                return;

            _animator.runtimeAnimatorController = controller;
            Initialize();
            _lastController = controller;
        }

        /// <summary>
        /// Checks to send animator data from server to clients.
        /// </summary>
        private void CheckSendToServer()
        {
            if (!_isActive)
                return;
            //Cannot send to server if is server.
            if (this.ReturnIsServer())
                return;
            //Cannot send to server if not client
            if (!this.ReturnIsClient())
                return;
            //Cannot send to server if not client authoritative or don't have authority.
            if (!ClientAuthoritative || !this.ReturnHasAuthority())
                return;

            //Not enough time passed to send.
            if (Time.time < _nextClientSendTime)
                return;
            _nextClientSendTime = Time.time + _synchronizeInterval;

            /* If there are updated parameters to send.
             * Don't really need to worry about mtu here
             * because there's no way the sent bytes are
             * ever going to come close to the mtu
             * when sending a single update. */
            if (AnimatorUpdated(out ArraySegment<byte> updatedBytes))
                FlexNetworkAnimatorManager.SendToServer(ReturnAnimatorUpdate(updatedBytes));
        }

        /// <summary>
        /// Checks to send animator data from server to clients.
        /// </summary>
        private void CheckSendToClients()
        {
            if (!_isActive)
                return;
            //Cannot send to clients if not server.
            if (!this.ReturnIsServer())
                return;
            //Not enough time passed to send.
            if (Time.time < _nextServerSendTime)
                return;

            bool sendFromServer;
            //If client authoritative.
            if (ClientAuthoritative)
            {
                //If has no owner then use latest values on server.
                if (!this.ReturnHasOwner())
                {
                    sendFromServer = true;
                }
                //If has a owner.
                else
                {
                    //If is owner then send latest values on server.
                    if (this.ReturnHasAuthority())
                    {
                        sendFromServer = true;
                    }
                    //Not owner.
                    else
                    {
                        //Haven't received any data from clients, cannot send yet.
                        if (_clientAuthoritativeUpdates.BufferCount == 0)
                            return;
                        //Has data to use, send from last received client value.
                        else
                            sendFromServer = false;
                    }
                }
            }
            //Not client authoritative, always send from server.
            else
            {
                sendFromServer = true;
            }

            _nextServerSendTime = Time.time + _synchronizeInterval;
            bool sendToAll = (_networkVisibility == null);
            /* If client authoritative then use what was received from clients
             * if data exist. */
            if (!sendFromServer)
            {
                byte[] buffer = null;
                int bufferLength = 0;
                for (int i = 0; i < _clientAuthoritativeUpdates.BufferCount; i++)
                {
                    _clientAuthoritativeUpdates.SetBuffer(i, ref buffer, ref bufferLength);
                    ArraySegment<byte> segment = new ArraySegment<byte>(buffer, 0, bufferLength);

                    if (sendToAll)
                    {
                        FlexNetworkAnimatorManager.SendToAll(ReturnAnimatorUpdate(segment));
                    }
                    else
                    {
#if MIRROR
                        foreach (NetworkConnection item in _networkVisibility.netIdentity.observers.Values)
#elif MIRAGE
                        foreach (INetworkConnection item in _networkVisibility.NetIdentity.observers)
#endif
                            FlexNetworkAnimatorManager.SendToObserver(item, ReturnAnimatorUpdate(segment));
                    }
                }
                //Reset client auth buffer.
                _clientAuthoritativeUpdates.Reset();
            }
            //Otherwise send what's changed.
            else
            {
                if (AnimatorUpdated(out ArraySegment<byte> updatedBytes))
                {
                    if (sendToAll)
                    {
                        FlexNetworkAnimatorManager.SendToAll(ReturnAnimatorUpdate(updatedBytes));
                    }
                    else
                    {
#if MIRROR
                        foreach (NetworkConnection item in _networkVisibility.netIdentity.observers.Values)
#elif MIRAGE
                        foreach (INetworkConnection item in _networkVisibility.NetIdentity.observers)
#endif
                            FlexNetworkAnimatorManager.SendToObserver(item, ReturnAnimatorUpdate(updatedBytes));
                    }
                }
            }
        }

        /// <summary>
        /// Returns a new AnimatorUpdate.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private AnimatorUpdate ReturnAnimatorUpdate(ArraySegment<byte> data)
        {
            return new AnimatorUpdate(CachedComponentIndex, this.ReturnNetId(), data);
        }
        /// <summary>
        /// Returns a new AnimatorUpdate.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private AnimatorUpdate ReturnAnimatorUpdate(byte[] data)
        {
            return new AnimatorUpdate(CachedComponentIndex, this.ReturnNetId(), new ArraySegment<byte>(data));
        }

        /// <summary>
        /// Smooths floats on clients.
        /// </summary>
        private void SmoothFloats()
        {
            //Don't need to smooth on authoritative client.
            if (!_canSmoothFloats)
                return;

            if (_smoothedFloats.Count > 0)
            {
                float deltaTime = Time.deltaTime;

                List<int> finishedEntries = new List<int>();

                /* Cycle through each target float and move towards it.
                    * Once at a target float mark it to be removed from floatTargets. */
                foreach (KeyValuePair<int, SmoothedFloat> item in _smoothedFloats)
                {
                    float current = _animator.GetFloat(item.Key);
                    float next = Mathf.MoveTowards(current, item.Value.Target, item.Value.Rate * deltaTime);
                    _animator.SetFloat(item.Key, next);

                    if (next == item.Value.Target)
                        finishedEntries.Add(item.Key);
                }

                //Remove finished entries from dictionary.
                for (int i = 0; i < finishedEntries.Count; i++)
                    _smoothedFloats.Remove(finishedEntries[i]);
            }
        }

        /// <summary>
        /// Returns if animator is updated and bytes of updated values.
        /// </summary>
        /// <returns></returns>
        private bool AnimatorUpdated(out ArraySegment<byte> updatedBytes, bool forceAll = false)
        {
            //Reset the writer.
            _writer.Reset();

            /* Every time a parameter is updated a byte is added
             * for it's index, this is why requiredBytes increases
             * by 1 when a value updates. ChangedParameter contains
             * the index updated and the new value. The requiresBytes
             * is increased also by however many bytes are required
             * for the type which has changed. Some types use special parameter
             * detail indexes, such as layer weights; these can be found under const. */
            for (byte parameterIndex = 0; parameterIndex < _parameterDetails.Count; parameterIndex++)
            {
                ParameterDetail pd = _parameterDetails[parameterIndex];
                /* Bool. */
                if (pd.ControllerParameter.type == AnimatorControllerParameterType.Bool)
                {
                    bool next = _animator.GetBool(pd.Hash);
                    //If changed.
                    if (forceAll || _bools[pd.TypeIndex] != next)
                    {
                        _writer.WriteByte(parameterIndex);
                        _writer.WriteBoolean(next);
                        _bools[pd.TypeIndex] = next;
                    }
                }
                /* Float. */
                else if (pd.ControllerParameter.type == AnimatorControllerParameterType.Float)
                {
                    float next = _animator.GetFloat(pd.Hash);
                    //If changed.
                    if (forceAll || _floats[pd.TypeIndex] != next)
                    {
                        _writer.WriteByte(parameterIndex);
                        Compressions.WriteCompressedFloat(_writer, next);
                        _floats[pd.TypeIndex] = next;
                    }
                }
                /* Int. */
                else if (pd.ControllerParameter.type == AnimatorControllerParameterType.Int)
                {
                    int next = _animator.GetInteger(pd.Hash);
                    //If changed.
                    if (forceAll || _ints[pd.TypeIndex] != next)
                    {
                        _writer.WriteByte(parameterIndex);
                        Compressions.WriteCompressedInt(_writer, next);
                        _ints[pd.TypeIndex] = next;
                    }
                }
            }

            /* Don't need to force trigger sends since
             * they're one-shots. */
            for (int i = 0; i < _triggerUpdates.Count; i++)
            {
                _writer.WriteByte(_triggerUpdates[i].ParameterIndex);
                _writer.WriteBoolean(_triggerUpdates[i].Setting);
            }
            _triggerUpdates.Clear();

            /* States. */
            if (forceAll)
            {
                //Add all layers to layer states.
                for (int i = 0; i < _animator.layerCount; i++)
                    _unsynchronizedLayerStates.Add(i);
            }
            //Go through each layer which needs to be synchronized.
            foreach (int layerIndex in _unsynchronizedLayerStates)
            {
                if (ReturnLayerState(out int stateHash, out float normalizedTime, layerIndex))
                {
                    _writer.WriteByte(STATE);
                    _writer.WriteByte((byte)layerIndex);
                    //hashes will always be too large to compress.
                    _writer.WriteInt32(stateHash);
                    Compressions.WriteCompressedFloat(_writer, normalizedTime);
                }
            }
            _unsynchronizedLayerStates.Clear();

            /* Layer weights. */
            for (int layerIndex = 0; layerIndex < _layerWeights.Length; layerIndex++)
            {
                float next = _animator.GetLayerWeight(layerIndex);
                if (forceAll || _layerWeights[layerIndex] != next)
                {
                    _writer.WriteByte(LAYER_WEIGHT);
                    _writer.WriteByte((byte)layerIndex);
                    Compressions.WriteCompressedFloat(_writer, next);
                    _layerWeights[layerIndex] = next;
                }
            }

            /* Speed is similar to layer weights but we don't need the index,
             * only the indicator and value. */
            float speedNext = _animator.speed;
            if (forceAll || _speed != speedNext)
            {
                _writer.WriteByte(SPEED);
                Compressions.WriteCompressedFloat(_writer, speedNext);
                _speed = speedNext;
            }

            //Nothing to update.
            if (_writer.Length == 0)
                return false;

            updatedBytes = _writer.ToArraySegment();
            return true;
        }

        /// <summary>
        /// Applies changed parameters to the animator.
        /// </summary>
        /// <param name="changedParameters"></param>
        private void ApplyParametersUpdated(ref ArraySegment<byte> updatedParameters)
        {
            if (!_isActive)
                return;
            if (updatedParameters.Array == null || updatedParameters.Array.Length == 0)
                return;
            //Exit if client authoritative and has authority.
            if (ClientAuthoritative && base.hasAuthority)
                return;
            //Exit if not client authoritative, but also not sync to owner, and is owner.
            if (!ClientAuthoritative && !_synchronizeToOwner && base.hasAuthority)
                return;
            //Exit if trying to apply when server and not client authoritative.
            if (base.isServer && !ClientAuthoritative)
                return;

            try
            {
                using (PooledNetworkReader reader = NetworkReaderPool.GetReader(updatedParameters))
                {
                    while (reader.Position < reader.Length)
                    {
                        byte parameterIndex = reader.ReadByte();
                        //Layer weight preset.
                        if (parameterIndex == LAYER_WEIGHT)
                        {
                            byte layerIndex = reader.ReadByte();
                            float value = Compressions.ReadCompressedFloat(reader);
                            _animator.SetLayerWeight((int)layerIndex, value);
                        }
                        //Speed preset.
                        else if (parameterIndex == SPEED)
                        {
                            float value = Compressions.ReadCompressedFloat(reader);
                            _animator.speed = value;
                        }
                        //State preset.
                        else if (parameterIndex == STATE)
                        {
                            byte layerIndex = reader.ReadByte();
                            //Hashes will always be too large to compress.
                            int hash = reader.ReadInt32();
                            float time = Compressions.ReadCompressedFloat(reader);
                            _animator.Play(hash, layerIndex, time);
                        }
                        //Not a preset index, is an actual parameter.
                        else
                        {
                            AnimatorControllerParameterType acpt = _parameterDetails[parameterIndex].ControllerParameter.type;
                            if (acpt == AnimatorControllerParameterType.Bool)
                            {
                                bool value = reader.ReadBoolean();
                                _animator.SetBool(_parameterDetails[parameterIndex].Hash, value);
                            }
                            //Float.
                            else if (acpt == AnimatorControllerParameterType.Float)
                            {
                                float value = Compressions.ReadCompressedFloat(reader);
                                //If able to smooth floats.
                                if (_canSmoothFloats)
                                {
                                    float currentValue = _animator.GetFloat(_parameterDetails[parameterIndex].Hash);
                                    float past = _synchronizeInterval + _interpolationFallbehind;
                                    float rate = Mathf.Abs(currentValue - value) / past;
                                    _smoothedFloats[_parameterDetails[parameterIndex].Hash] = new SmoothedFloat(rate, value);
                                }
                                else
                                {
                                    _animator.SetFloat(_parameterDetails[parameterIndex].Hash, value);
                                }
                            }
                            //Integer.
                            else if (acpt == AnimatorControllerParameterType.Int)
                            {
                                int value = Compressions.ReadCompressedInt(reader);
                                _animator.SetInteger(_parameterDetails[parameterIndex].Hash, value);
                            }
                            //Trigger.
                            else if (acpt == AnimatorControllerParameterType.Trigger)
                            {
                                bool value = reader.ReadBoolean();
                                if (value)
                                    _animator.SetTrigger(_parameterDetails[parameterIndex].Hash);
                                else
                                    _animator.ResetTrigger(_parameterDetails[parameterIndex].Hash);
                            }
                        }
                    }
                }
            }
            catch
            {
                Debug.LogWarning("An error occurred while applying updates. This may occur when malformed data is sent or when you change the animator or controller but not on all connections.");
            }
        }

        /// <summary>
        /// Outputs the current state and time for a layer. Returns true if stateHash is not 0.
        /// </summary>
        /// <param name="stateHash"></param>
        /// <param name="normalizedTime"></param>
        /// <param name="results"></param>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        private bool ReturnLayerState(out int stateHash, out float normalizedTime, int layerIndex)
        {
            stateHash = 0;
            normalizedTime = 0f;
            if (!_isActive)
                return false;

            AnimatorStateInfo st = _animator.GetCurrentAnimatorStateInfo(layerIndex);
            stateHash = st.fullPathHash;
            normalizedTime = st.normalizedTime;

            return (stateHash != 0);
        }

        #region Play.
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void Play(int hash)
        {
            for (int i = 0; i < _animator.layerCount; i++)
                Play(hash, i, 0f);
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void Play(string name)
        {
            Play(Animator.StringToHash(name));
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void Play(int hash, int layer)
        {
            Play(hash, layer, 0f);
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void Play(string name, int layer)
        {
            Play(Animator.StringToHash(name), layer);
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void Play(string name, int layer, float normalizedTime)
        {
            Play(Animator.StringToHash(name), layer, normalizedTime);
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void Play(int hash, int layer, float normalizedTime)
        {
            if (_animator.HasState(layer, hash))
            {
                _animator.Play(hash, layer, normalizedTime);
                _unsynchronizedLayerStates.Add(layer);
            }
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void PlayInFixedTime(int hash)
        {
            for (int i = 0; i < _animator.layerCount; i++)
                PlayInFixedTime(hash, i);
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void PlayInFixedTime(string name)
        {
            PlayInFixedTime(Animator.StringToHash(name));
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void PlayInFixedTime(int hash, int layer)
        {
            PlayInFixedTime(hash, layer, 0f);
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void PlayInFixedTime(string name, int layer)
        {
            PlayInFixedTime(Animator.StringToHash(name), layer);
        }
        /// <summary>
        /// Plays a state.
        /// </summary>
        public void PlayInFixedTime(int hash, int layer, float fixedTime)
        {
            if (_animator.HasState(layer, hash))
            {
                _animator.PlayInFixedTime(hash, layer, fixedTime);
                _unsynchronizedLayerStates.Add(layer);
            }
        }
        #endregion

        #region Crossfade.
        /// <summary>
        /// Creates a crossfade from the current state to any other state using normalized times.
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="normalizedTransitionDuration"></param>
        /// <param name="layer"></param>
        /// <param name="normalizedTimeOffset"></param>
        /// <param name="normalizedTransitionTime"></param>
        public void CrossFade(string stateName, float normalizedTransitionDuration, int layer = -1, float normalizedTimeOffset = float.NegativeInfinity, float normalizedTransitionTime = 0.0f)
        {
            CrossFade(Animator.StringToHash(stateName), normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }
        /// <summary>
        /// Creates a crossfade from the current state to any other state using normalized times.
        /// </summary>
        /// <param name="stateHashName"></param>
        /// <param name="normalizedTransitionDuration"></param>
        /// <param name="layer"></param>
        /// <param name="normalizedTimeOffset"></param>
        /// <param name="normalizedTransitionTime"></param>
        public void CrossFade(int stateHashName, float normalizedTransitionDuration, int layer = -1, float normalizedTimeOffset = 0.0f, float normalizedTransitionTime = 0.0f)
        {
            _animator.CrossFade(stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
            _unsynchronizedLayerStates.Add(layer);
        }
        /// <summary>
        /// Creates a crossfade from the current state to any other state using times in seconds.
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="fixedTransitionDuration"></param>
        /// <param name="layer"></param>
        /// <param name="fixedTimeOffset"></param>
        /// <param name="normalizedTransitionTime"></param>
        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer = -1, float fixedTimeOffset = 0.0f, float normalizedTransitionTime = 0.0f)
        {
            CrossFadeInFixedTime(Animator.StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }
        /// <summary>
        /// Creates a crossfade from the current state to any other state using times in seconds.
        /// </summary>
        /// <param name="stateHashName"></param>
        /// <param name="fixedTransitionDuration"></param>
        /// <param name="layer"></param>
        /// <param name="fixedTimeOffset"></param>
        /// <param name="normalizedTransitionTime"></param>
        public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration, int layer = -1, float fixedTimeOffset = 0.0f, float normalizedTransitionTime = 0.0f)
        {
            _animator.CrossFadeInFixedTime(stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
            _unsynchronizedLayerStates.Add(layer);
        }
        #endregion

        #region Triggers.
        /// <summary>
        /// Sets a trigger on the animator and sends it over the network.
        /// </summary>
        /// <param name="hash"></param>
        public void SetTrigger(int hash)
        {
            if (!_isActive)
                return;

            UpdateTrigger(hash, true);
        }
        /// <summary>
        /// Sets a trigger on the animator and sends it over the network.
        /// </summary>
        /// <param name="hash"></param>
        public void SetTrigger(string name)
        {
            if (!_isActive)
                return;

            SetTrigger(Animator.StringToHash(name));
        }

        /// <summary>
        /// Resets a trigger on the animator and sends it over the network.
        /// </summary>
        /// <param name="hash"></param>
        public void ResetTrigger(int hash)
        {
            if (!_isActive)
                return;

            UpdateTrigger(hash, false);
        }
        /// <summary>
        /// Resets a trigger on the animator and sends it over the network.
        /// </summary>
        /// <param name="hash"></param>
        public void ResetTrigger(string name)
        {
            ResetTrigger(Animator.StringToHash(name));
        }

        /// <summary>
        /// Updates a trigger, sets or resets.
        /// </summary>
        /// <param name="set"></param>
        private void UpdateTrigger(int hash, bool set)
        {
            /* Allow triggers to run on owning client if using client authority,
             * as well when not using client authority but also not using synchronize to owner.
             * This allows clients to run animations locally while maintaining server authority. */
            //Using client authority but not owner.
            if (ClientAuthoritative && !this.ReturnHasAuthority())
                return;

            //Also block if not using client authority, synchronizing to owner, and not server.
            if (!ClientAuthoritative && _synchronizeToOwner && !this.ReturnIsServer())
                return;

            //Update locally.
            if (set)
                _animator.SetTrigger(hash);
            else
                _animator.ResetTrigger(hash);

            /* Can send if not client auth but is server,
            * or if client auth and owner. */
            bool canSend = (!ClientAuthoritative && this.ReturnIsServer()) ||
                (ClientAuthoritative && this.ReturnHasAuthority());
            //Only queue a send if proper side.
            if (canSend)
            {
                for (byte i = 0; i < _parameterDetails.Count; i++)
                {
                    if (_parameterDetails[i].Hash == hash)
                    {
                        _triggerUpdates.Add(new TriggerUpdate(i, set));
                        return;
                    }
                }
                //Fall through, hash not found.
                Debug.LogWarning("Hash " + hash + " not found while trying to update a trigger.");
            }
        }
        #endregion

        /// <summary>
        /// Called on server when client data is received.
        /// </summary>
        /// <param name="data"></param>
        public void ClientDataReceived(ref AnimatorUpdate au)
        {
            if (!_isActive)
                return;
            if (!ClientAuthoritative)
                return;

            ApplyParametersUpdated(ref au.Data);
            _clientAuthoritativeUpdates.AddToBuffer(ref au.Data);
        }

        /// <summary>
        /// Called on clients when server data is received.
        /// </summary>
        /// <param name="data"></param>
        public void ServerDataReceived(ref AnimatorUpdate au)
        {
            if (!_isActive)
                return;
            //If also server, client host, then do nothing. Animations already ran on server.
            if (this.ReturnIsServer())
                return;

            //If has authority.
            if (this.ReturnHasAuthority())
            {
                //No need to sync to self if client authoritative.
                if (ClientAuthoritative)
                    return;
                //Not client authoritative, but also don't sync to owner.
                else if (!ClientAuthoritative && !_synchronizeToOwner)
                    return;
            }

            ApplyParametersUpdated(ref au.Data);
        }


    }
}

