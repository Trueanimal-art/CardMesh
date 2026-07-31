using UnityEngine;

namespace CardMesh
{
    public sealed class ShopProductButton : MonoBehaviour
    {
        [SerializeField] private string productName;
        [SerializeField] private string price;
        [SerializeField] private string purchaseType;

        public string ProductName => productName;
        public string Price => price;
        public string PurchaseType => purchaseType;

        public void Configure(string productNameValue, string priceValue, string purchaseTypeValue)
        {
            productName = productNameValue;
            price = priceValue;
            purchaseType = purchaseTypeValue;
        }
    }
}
