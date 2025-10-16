using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recoil : MonoBehaviour
{
    private WeaponData weaponData;
    private WeaponAttachmentSystem attachmentSystem;
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private float recoilX;
    private float recoilY;
    private float recoilZ;

    [SerializeField] private float snappiness;
    [SerializeField] private float returnSpeed;

    void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, snappiness * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void RecoilFire()
    {
        targetRotation += new Vector3(recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilZ));
    }

    public void SetWeaponData(WeaponData data)
    {
        weaponData = data;
        if (weaponData != null)
        {
            recoilX = weaponData.recoilX;
            recoilY = weaponData.recoilY;
            recoilZ = weaponData.recoilZ;
        }
        else
        {
            recoilX = recoilY = recoilZ = 0f;
        }
    }

    public void SetAttachmentSystem(WeaponAttachmentSystem attachSys)
    {
        attachmentSystem = attachSys;
        UpdateRecoilFromAttachments();
    }

    private void UpdateRecoilFromAttachments()
    {
        if (attachmentSystem != null)
        {
            // Use modified recoil values from attachments
            recoilX = attachmentSystem.CurrentRecoilX;
            recoilY = attachmentSystem.CurrentRecoilY;
            recoilZ = attachmentSystem.CurrentRecoilZ;
            Debug.Log($"Recoil updated from attachments: ({recoilX}, {recoilY}, {recoilZ})");
        }
        else if (weaponData != null)
        {
            // Fall back to base weapon data
            recoilX = weaponData.recoilX;
            recoilY = weaponData.recoilY;
            recoilZ = weaponData.recoilZ;
        }
        else
        {
            recoilX = recoilY = recoilZ = 0f;
        }
    }
}