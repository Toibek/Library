using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class IapManager : MonoBehaviour , IStoreListener ,IStoreController
{
    public ProductCollection products => throw new NotImplementedException();

    public void ConfirmPendingPurchase(Product product)
    {
        throw new NotImplementedException();
    }

    public void FetchAdditionalProducts(HashSet<ProductDefinition> products, Action successCallback, Action<InitializationFailureReason> failCallback)
    {
        throw new NotImplementedException();
    }

    public void InitiatePurchase(Product product, string payload)
    {
        throw new NotImplementedException();
    }

    public void InitiatePurchase(string productId, string payload)
    {
        throw new NotImplementedException();
    }

    public void InitiatePurchase(Product product)
    {
        throw new NotImplementedException();
    }

    public void InitiatePurchase(string productId)
    {
        throw new NotImplementedException();
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        throw new System.NotImplementedException();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        throw new System.NotImplementedException();
    }

    public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
    {
        throw new System.NotImplementedException();
    }
    
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
