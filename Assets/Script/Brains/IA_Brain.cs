﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IA_Brain : Brain {

    private IA_State myIAState;
    [SerializeField] string IADebugState;
    [SerializeField] string IADebugStateFct;

    public IA_State MyIAState
    {
        get
        {
            return myIAState;
        }

        set
        {
            if (myIAState != null)
                myIAState.OnEnd();
            myIAState = value;
            if (myIAState != null)
                myIAState.OnBegin();
            else
                Debug.Log("myIAState est null t'es sur ?");

            IADebugState = myIAState.ToString();
        }
    }
    public override void Awake()
    {
        base.Awake();
        if(puppet.GetComponent<Alpha>() == null)
        {
            MyIAState = new Follow_State(puppet);
        }
        else
        {

        }
    }
    // Use this for initialization
    public override void Start () {
		
	}

    public override void Update()
    {
        if (MyIAState != null && MyIAState.UpdateFct != null) 
            MyIAState.UpdateFct();
    }
    public override void FixedUpdate()
    {
        if (MyIAState != null && MyIAState.FixedUpdateFct != null) 
            MyIAState.FixedUpdateFct();
    }
}
