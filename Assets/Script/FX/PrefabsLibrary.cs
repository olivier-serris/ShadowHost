﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "Libraries/SFXLibrary", order = 1)]
public class PrefabsLibrary : ScriptableObject 
{
	public GameObject FX_Host;
	public GameObject FX_OnDeadSelectable;

	public GameObject FX_OneShotPossession;
	public Material MAT_OnDeadSelectable;
}
