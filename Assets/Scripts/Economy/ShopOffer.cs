using UnityEngine;

public enum OfferRewardType
{
    ExtraMoves
}

public enum OfferPaymentMethod
{
    Coins,
    RewardedAd
}

[CreateAssetMenu(fileName = "ShopOffer", menuName = "ScriptableObjects/Shop Offer", order = 2)]
public class ShopOffer : ScriptableObject
{
    public string offerId = "continue_extra_moves";
    public int coinCost = 300;
    public OfferRewardType rewardType = OfferRewardType.ExtraMoves;
    public int rewardAmount = 3;
    public OfferPaymentMethod[] allowedPayments = { OfferPaymentMethod.Coins, OfferPaymentMethod.RewardedAd };
}
