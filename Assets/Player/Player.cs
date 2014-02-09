﻿using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    public enum PlayerType
    {
        Host,
        Client
    }
    public PlayerType Type;
    public float MoveSpeed = 12;
    public float RotationSpeed = Mathf.PI;
    CharacterController Character;

    Transform Head;

    void EnableOculusCameras()
    {
        Camera[] cameras = GetComponentsInChildren<Camera>();
        foreach (Camera c in cameras)
            c.enabled = true;

        OVRCameraController camcontroller = GetComponentInChildren<OVRCameraController> ();
        camcontroller.enabled = true;

        OVRDevice device = GetComponentInChildren<OVRDevice>();
        device.enabled = true;

        OVRCamera[] ovrcameras = GetComponentsInChildren<OVRCamera>();
        foreach (OVRCamera c in ovrcameras)
            c.enabled = true;

        OVRLensCorrection[] lens = GetComponentsInChildren<OVRLensCorrection>();
        foreach (OVRLensCorrection l in lens)
            l.enabled = true;

        AudioListener listener = GetComponentInChildren<AudioListener>();
        listener.enabled = true;
    }


    void OnConnectedToServer()
    {
        if (Type == PlayerType.Client)
        {
            EnableOculusCameras();
        }
    }

    void OnServerInitialized()
    {
        if (Type == PlayerType.Host)
        {
            EnableOculusCameras();
        }
    }


    SixenseHands PlayerHand
    {
        get { return (Type == PlayerType.Host ? SixenseHands.LEFT : SixenseHands.RIGHT); }
    }

	// Use this for initialization
	void Start () 
    {
        Head = transform.FindChild("OVRCameraController/CameraLeft/Head");

        Character = GetComponent<CharacterController>();
	}
	
	// Update is called once per frame
	void Update () 
    {
        HandleOculusResets();


        if (Type == PlayerType.Client && Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("REMOVE HEAD");
            Decapitate();
        }

        if (Network.isServer)
        {
            SixenseInput.Controller controller = SixenseInput.GetController(PlayerHand);
            if (controller == null || !controller.Enabled)
                return;

            Vector3 move = new Vector3(0, 0, controller.JoystickY) * MoveSpeed;

            if(controller.Trigger > 0.2) {
				move.x = controller.JoystickX * MoveSpeed;
			} else {
				Vector3 angle = transform.rotation.ToEulerAngles();
				angle.y += controller.JoystickX * RotationSpeed * Time.deltaTime;
				transform.rotation = Quaternion.EulerAngles(angle);
			}

			move = transform.rotation * move;
            Character.SimpleMove(move);

            
        }


        
	}

    void HandleOculusResets()
    {
        if (Input.GetKey(KeyCode.B) && (Network.isClient && Type == PlayerType.Client ||  Network.isServer && Type == PlayerType.Host))
        {
            OVRDevice.ResetOrientation(0);
            Debug.Log("Resetting Oculus");
        }
    }

    void RemoveHead()
    {

        Vector3 whpos = Head.position;
        Quaternion whrot = Head.rotation;

        Head.parent = null;

        Transform OVRcam = transform.FindChild("OVRCameraController");

        OVRcam.parent = Head;

        Head.gameObject.AddComponent<Rigidbody>();

        Head.rigidbody.AddForce(Head.rotation * new Vector3(0, 1, 1));

        Head.networkView.stateSynchronization = NetworkStateSynchronization.Unreliable;
        Head.networkView.observed = Head.rigidbody;



    }

    [RPC]
    void RemoteDecapitate()
    {
        RemoveHead();
    }

    void Decapitate()
    {
        if (Network.isServer)
        {
            networkView.RPC("RemoteDecapitate", RPCMode.AllBuffered);
            RemoveHead();
        }
    }

   
}
