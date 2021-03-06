﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class PlayerSwordController : SwordController
{
    public enum PlayerType
    {
        Host,
        Client
    }
    public PlayerType Type;

    SixenseHands PlayerHand
    {
        get { return (Type == PlayerType.Host ? SixenseHands.LEFT : SixenseHands.RIGHT); }
    }

    enum SyncMode
    {
        Starting,
        Chest,
        Waiting,
        ArmLength,
        Done
    }
    SyncMode Mode = SyncMode.Starting;

    Vector3 ChestPosition;
    Vector3 ArmLengthPosition;
    float Scale;

    public PlayerSwordController()
    {
    }

    public override bool Ready() 
    {
        return Mode == SyncMode.Done;
    }


    void Update()
    {
        if (Network.isServer && Type == PlayerType.Host) 
        {
            if (Input.GetKey(KeyCode.C))
            {
                Debug.Log("Resetting P1 Hydra");
                Mode = SyncMode.Starting;
            }
        }
        if (Network.isServer && Type == PlayerType.Client)
        {
            if (Input.GetKey(KeyCode.V))
            {
                Debug.Log("Resetting P2 Hydra");
                Mode = SyncMode.Starting;
            }
        }
    }

    public override Orientation GetOrientation(Transform anchor) 
    {
        if (Network.isServer)
        {
            SixenseInput.Controller controller = SixenseInput.GetController(PlayerHand);
            if (controller == null || !controller.Enabled)
                return new Orientation(new Vector3(), new Quaternion());

            //Debug.Log("Player");

            if (Mode == SyncMode.Starting)
            {
                if (controller.Trigger <= 0)
                {
                    Mode = SyncMode.Chest;
                }
                return new Orientation(new Vector3(), new Quaternion());
            }
            if (Mode == SyncMode.Chest)
            {
                if (controller.Trigger > 0.5)
                {
                    ChestPosition = controller.Position;
                    Mode = SyncMode.Waiting;
                }
                return new Orientation(new Vector3(), new Quaternion());
            }
            else if (Mode == SyncMode.Waiting)
            {
                if (controller.Trigger <= 0)
                {
                    Mode = SyncMode.ArmLength;
                }
                return new Orientation(new Vector3(), new Quaternion());
            }
            else if (Mode == SyncMode.ArmLength)
            {
                if (controller.Trigger > 0.5)
                {
                    ArmLengthPosition = controller.Position;
                    Mode = SyncMode.Done;
                }
                return new Orientation(new Vector3(), new Quaternion());
            }

            Scale = (ArmLengthPosition - ChestPosition).magnitude;

            return new Orientation(anchor.position + new Vector3(0, 1.54f, 0) + anchor.rotation * ((controller.Position - ChestPosition) / Scale), //Magical arm constant
                                    anchor.rotation * controller.Rotation);
        }
        return new Orientation(new Vector3(), new Quaternion());

    }



    public void OnGUI()
    {
        if ((Network.isServer && Type == PlayerType.Host) || (Network.isClient && Type == PlayerType.Client))
        {
            if (Mode == SyncMode.Chest)
            {
                GUIHelper.StereoMessage("Place Controller At Chest and Pull Trigger");
            }
            if (Mode == SyncMode.ArmLength)
            {
                GUIHelper.StereoMessage("Place Controller At Arms Length and Pull Trigger");
            }
        }
    }

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        int mode = (int)Mode;
        stream.Serialize(ref mode);
        Mode = (SyncMode)mode;
    }
}

