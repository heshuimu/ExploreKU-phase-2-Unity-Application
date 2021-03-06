﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ExploreKu.UnityComponents.DataProcessing;
using ExploreKu.DataClasses;

public class MainSceneController : MonoBehaviour {

	public float refreshDistance = 20f;
	public float labelVisibleRadius = 1f;

	[SerializeField]
	private Camera mainCamera;
	[SerializeField]
	private GameObject locationLabelTemplate;
	[SerializeField]
	private Text gpsLabel;

	private List<LocationLabelController> spawnedControllers = new List<LocationLabelController>();
	private Vector2? lastTriggeredLocation = null;
	bool isLocationStarted = false;

	bool isRefreshInProgress = false;

	void CheckLocationService()
	{
		if (!Input.location.isEnabledByUser)
		{
			isLocationStarted = false;
			return;
		}

		if(!isLocationStarted)
		{
			Input.location.Start();
			isLocationStarted = true;
		}
	}

	void Start()
	{
		CheckLocationService();
	}

	void Update()
	{
		CheckLocationService();
		if(Input.location.status != LocationServiceStatus.Running)
			return;

		gpsLabel.text = "Running!!";
		LocationInfo info = Input.location.lastData;
		gpsLabel.text = info.longitude + ", " + info.latitude + ","  + info.altitude;

		Vector2 currentGeocoord = new Vector2(info.latitude, info.longitude);

		GPSEncoder.SetLocalOrigin(currentGeocoord);

		if(isRefreshInProgress)
			return;

		if(lastTriggeredLocation == null || Vector3.Distance(lastTriggeredLocation.Value, currentGeocoord) > refreshDistance)
		{
			lastTriggeredLocation = currentGeocoord;
			DataProcessTool.Instance.GetLocationsInRange(info.longitude, info.latitude, labelVisibleRadius, WhenDataArrives);
			isRefreshInProgress = true;
		}

		ExploreKuStateSaver.currentLocation = new GeographicCoordinate{longitude = info.longitude, latitude = info.latitude, altitude = info.altitude};
	}

	void WhenDataArrives(Location[] l)
	{
		AdjustCameraHeight(l);
		RefreshAllLabels(l);
	}

	void AdjustCameraHeight(Location[] locations)
	{
		if(locations.Length == 0) return;

		List<float> allAltitudes = new List<float>();
		foreach(Location l in locations)
		{
			allAltitudes.Add(l.altitudeFloat);
		}
		allAltitudes.Sort();

		int total = allAltitudes.Count;
		int floor = total > 2 ? (int)(total * 0.1f) : 0;
		int ceiling = total > 2 ? (int)(total * 0.9f) : total;

		float sum = 0;
		int i;
		for(i =  floor; i < ceiling; i++)
		{
			print(allAltitudes[i]);
			sum += allAltitudes[i];
		}

		mainCamera.transform.localPosition = new Vector3(0, sum / (ceiling - floor), 0);
	}

	void RefreshAllLabels(Location[] locations)
	{
		while(spawnedControllers.Count < locations.Length)
		{
			GameObject go = Instantiate(locationLabelTemplate);
			spawnedControllers.Add(go.GetComponent<LocationLabelController>());
		}

		int counter = 0;
		foreach(Location l in locations)
		{
			spawnedControllers[counter].SetVisible(true);
			spawnedControllers[counter].RefreshLabel(l);
			counter++;
		}

		while(counter < spawnedControllers.Count)
		{
			spawnedControllers[counter].SetVisible(false);
			counter++;
		}
	}
}
