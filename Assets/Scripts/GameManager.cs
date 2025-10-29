using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	void Update()
	{
        if (SystemInput.GetKey(KeyCode.Escape))
        {
			Quit();
		}
	}

	public void Quit()
	{
		Application.Quit();
	}
}
