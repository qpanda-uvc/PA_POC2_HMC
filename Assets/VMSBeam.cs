using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VLB;
using PGD;

public class VMSBeam : MonoBehaviour
{
    NetworkManager networkManager;

    public VolumetricLightBeamSD[] beamLights;
    public float startSpotAngle;
    public float targetSpotAngle;
    public float scanningSpeed;
    public float endSpeed;
    public float startX;
    public float targetX;
    public int scannedIndex;
    public Panel_011 floatingUI;
    public Panel_014 VMSPanelUI;
    public Cargo scanningCargo;
         
    private void Awake()
    {
        networkManager = networkManager = GameObject.FindObjectOfType<NetworkManager>();
        beamLights = GetComponentsInChildren<VolumetricLightBeamSD>();
        foreach (var b in beamLights)
        {
            b.spotAngle = startSpotAngle;
            b.enabled = false;
        }

        scannedIndex = 0;
    }

    IEnumerator Scanning()
    {
        ActivateBeam();
        float timer = 0f;
        while (timer < scanningSpeed)
        {
            timer += scanningProcess;
            foreach (var b in beamLights)
            {
                b.spotAngle = targetSpotAngle * (timer / scanningSpeed);
            }
            yield return null;
        }

        timer = 0f;
        while (timer < scanningSpeed)
        {
            timer += scanningProcess;
            foreach(var b in beamLights)
            {
                var r = b.transform.eulerAngles;
                r.x = Mathf.Lerp(startX, targetX, (timer / scanningSpeed));
                b.transform.eulerAngles = r;    
            }
            yield return null;
        }

        StartCoroutine(ReScanning());
    }

    IEnumerator Scanning2()
    {
        float timer = 0f;
        while (timer < scanningSpeed)
        {
            timer += scanningProcess;
            foreach (var b in beamLights)
            {
                var r = b.transform.eulerAngles;
                r.x = Mathf.Lerp(startX, targetX, (timer / scanningSpeed));
                b.transform.eulerAngles = r;
            }
            yield return null;
        }
        StartCoroutine(ReScanning());
    }
    IEnumerator ReScanning()
    {
        float timer = 0f;
        while(timer < scanningSpeed)
        {
            timer += scanningProcess;
            foreach(var b in beamLights)
            {
                var r = b.transform.eulerAngles;
                r.x = Mathf.Lerp(targetX, startX, timer / scanningSpeed);
                b.transform.eulerAngles = r;
            }
            yield return null;
        }
        StartCoroutine(Scanning2());
    }

    float scanningProcess => scanningSpeed * Time.deltaTime;
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Cargo>(out var aks))
        {
            scannedIndex++;
            scanningCargo = aks;
            StopAllCoroutines();
            StartCoroutine(Scanning());
            if (floatingUI != null) floatingUI.UpdateVMSData("1", "������", scannedIndex);
            if (UIManager.Instance != null) UIManager.Instance.UpdateVMSPanelData(
                aks.cargoName,
                "����",
                aks.waterVolume,
                aks.weight,
                aks.POU,
                aks.SCCs.ToArray());

            
        }
  
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Cargo>(out var aks))
        {
            StopAllCoroutines();
            StartCoroutine(EndScanning());
            scanningCargo = null;
            if (floatingUI != null) floatingUI.UpdateVMSData("1", "�����", scannedIndex);

            VMSAwbInfo vmsAwb = new VMSAwbInfo(
                aks.cargoID,
                "3d Model Name",
                aks.waterVolume,
                1,
                aks.width,
                aks.length,
                aks.depth,
                aks.weight,
                1,
                "saved",
                0,
                " ",
                true,
                "/c/file/xxx",
                1,
                "��ۼ���",
                aks.SCCs.ToArray(),
                21);

            string json = JsonUtility.ToJson(vmsAwb);
            if (networkManager != null) networkManager.PostVMSAwb(json);
        }

    }

    IEnumerator EndScanning()
    {
        float timer = 0f;
        while (timer < endSpeed)
        {
            timer += endSpeed * Time.deltaTime;
            
            yield return null;
        }
        DeactiveBeam();
    }
    void DeactiveBeam()
    {
        foreach (var b in beamLights)
        {
            b.enabled = false;
        }
    }
    void ActivateBeam()
    {
        foreach (var b in beamLights)
        {
            b.enabled = true;
            b.spotAngle = startSpotAngle;
            var a = b.transform.eulerAngles;
            a.x = startX;
            b.transform.eulerAngles = a;
        }
    }
}
