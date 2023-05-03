using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MixLevels : MonoBehaviour
{
	public AudioMixer masterMixer;

	public void SetMaster(float masterlvl)
	{
		masterMixer.SetFloat("Master", masterlvl);
	}

	public void SetMusic(float musiclvl)
	{
		masterMixer.SetFloat("Music", musiclvl);
	}

	public void SetPlayer(float playerlvl)
	{
		masterMixer.SetFloat("Player", playerlvl);
	}

	public void SetZombies(float zombieslvl)
	{
		masterMixer.SetFloat("Zombies", zombieslvl);
	}

	public void SetUI(float UIlvl)
	{
		masterMixer.SetFloat("UI", UIlvl);
	}

	public void SetScene(float scenelvl)
	{
		masterMixer.SetFloat("Scene", scenelvl);
	}

}
