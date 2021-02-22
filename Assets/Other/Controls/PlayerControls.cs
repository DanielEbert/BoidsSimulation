// GENERATED AUTOMATICALLY FROM 'Assets/Other/Controls/PlayerControls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class PlayerControls : IInputActionCollection
{
    private InputActionAsset asset;
    public PlayerControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerControls"",
    ""maps"": [
        {
            ""name"": ""Gameplay"",
            ""id"": ""95c9d3ff-2185-4234-894e-9954b9739887"",
            ""actions"": [
                {
                    ""name"": ""Rotate"",
                    ""id"": ""d9fd2917-701a-4935-9fe3-e735ec9057fb"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""Accelerate"",
                    ""id"": ""ef07314e-401d-447a-836a-237251f45aec"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""Break"",
                    ""id"": ""7ea282e9-e9c3-47d5-8797-9a116b73f863"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""RotateCam"",
                    ""id"": ""ef89b8ec-7719-48b9-bb00-eb75bcbe0a45"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""725aea5e-e734-4729-9473-a3d06b033d46"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""a5d672dd-8c21-477e-8376-de4c6174c22e"",
                    ""path"": ""<Gamepad>/rightTrigger"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Accelerate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""0d849e69-4630-419c-ac4b-6ce5887a63a3"",
                    ""path"": ""<Gamepad>/leftTrigger"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Break"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""4e9ab030-7f1b-49e5-934e-6fb18ce94c7c"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""RotateCam"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Gameplay
        m_Gameplay = asset.GetActionMap("Gameplay");
        m_Gameplay_Rotate = m_Gameplay.GetAction("Rotate");
        m_Gameplay_Accelerate = m_Gameplay.GetAction("Accelerate");
        m_Gameplay_Break = m_Gameplay.GetAction("Break");
        m_Gameplay_RotateCam = m_Gameplay.GetAction("RotateCam");
    }

    ~PlayerControls()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes
    {
        get => asset.controlSchemes;
    }

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Gameplay
    private InputActionMap m_Gameplay;
    private IGameplayActions m_GameplayActionsCallbackInterface;
    private InputAction m_Gameplay_Rotate;
    private InputAction m_Gameplay_Accelerate;
    private InputAction m_Gameplay_Break;
    private InputAction m_Gameplay_RotateCam;
    public struct GameplayActions
    {
        private PlayerControls m_Wrapper;
        public GameplayActions(PlayerControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Rotate { get { return m_Wrapper.m_Gameplay_Rotate; } }
        public InputAction @Accelerate { get { return m_Wrapper.m_Gameplay_Accelerate; } }
        public InputAction @Break { get { return m_Wrapper.m_Gameplay_Break; } }
        public InputAction @RotateCam { get { return m_Wrapper.m_Gameplay_RotateCam; } }
        public InputActionMap Get() { return m_Wrapper.m_Gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled { get { return Get().enabled; } }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
        public void SetCallbacks(IGameplayActions instance)
        {
            if (m_Wrapper.m_GameplayActionsCallbackInterface != null)
            {
                Rotate.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotate;
                Rotate.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotate;
                Rotate.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotate;
                Accelerate.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnAccelerate;
                Accelerate.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnAccelerate;
                Accelerate.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnAccelerate;
                Break.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnBreak;
                Break.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnBreak;
                Break.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnBreak;
                RotateCam.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotateCam;
                RotateCam.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotateCam;
                RotateCam.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotateCam;
            }
            m_Wrapper.m_GameplayActionsCallbackInterface = instance;
            if (instance != null)
            {
                Rotate.started += instance.OnRotate;
                Rotate.performed += instance.OnRotate;
                Rotate.canceled += instance.OnRotate;
                Accelerate.started += instance.OnAccelerate;
                Accelerate.performed += instance.OnAccelerate;
                Accelerate.canceled += instance.OnAccelerate;
                Break.started += instance.OnBreak;
                Break.performed += instance.OnBreak;
                Break.canceled += instance.OnBreak;
                RotateCam.started += instance.OnRotateCam;
                RotateCam.performed += instance.OnRotateCam;
                RotateCam.canceled += instance.OnRotateCam;
            }
        }
    }
    public GameplayActions @Gameplay
    {
        get
        {
            return new GameplayActions(this);
        }
    }
    public interface IGameplayActions
    {
        void OnRotate(InputAction.CallbackContext context);
        void OnAccelerate(InputAction.CallbackContext context);
        void OnBreak(InputAction.CallbackContext context);
        void OnRotateCam(InputAction.CallbackContext context);
    }
}
