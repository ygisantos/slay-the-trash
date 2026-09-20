using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class CardCollectionSceneScript : MonoBehaviour
{
    public GameObject noCards;
    public Vector2 cardDistance;
    public float bottomPad = 20f;
    public int yOffset = 334;
    public RectTransform canvasParent;
    public List<string> cardNames;
    public List<GameObject> actualCards;
    public List<GameObject> cardPrefabs;

    private List<string> cards;

    void Start()
    {
        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Cannot load card collection without a logged-in username.");
            return;
        }

        FBCardCollection.Instance.GetCards(
            username,
            entries =>
            {
                // Keep the "trashType:cardName:rarity" shape ConvertToPrefabs already expects.
                cards = entries
                    .Select(entry => $"{entry.trashType}:{entry.cardName}:{entry.rarity}")
                    .ToList();

                StartCoroutine(ConvertToPrefabs());
            },
            error => Debug.LogError($"Failed to load card collection: {error}")
        );
    }

    private string GetCurrentUsername()
    {
        Dictionary<string, object> profile =
            FBAuthentication.Instance != null
                ? FBAuthentication.Instance.CurrentProfile
                : null;

        if (profile == null && DataManager.Instance != null)
            profile = DataManager.Instance.GetProfile();

        return FirebaseDataHelper.GetString(profile, "username");
    }

    private IEnumerator ConvertToPrefabs()
    {
        foreach (string card in cards)
        {
            string[] c = card.Split(':');
            cardNames.Add(c[1]);
        }

        int startingYInitial = cardNames.Count / 5 ;
        startingYInitial = cardNames.Count % 5 > 0 ? startingYInitial + 1 : startingYInitial;
        float startingY = cardDistance.y * (float)startingYInitial / 2f - yOffset;
        canvasParent.sizeDelta = new Vector2(canvasParent.sizeDelta.x, cardDistance.y * startingYInitial + bottomPad);

        yield return new WaitForSeconds(.2f);
        //THEN CONVERT TO ACTUAL CARDS
        if (cardNames.Count > 0 && (cardPrefabs != null && cardPrefabs.Count == 18)) // card prefabs is 18 currently, change this shit later
        {
            for (int i = 0; i < cardNames.Count; i++)
            {
                int x = (i % 5) - 2;
                int y = -(i / 5);
                switch (cardNames[i])
                {
                    case "Crumpled":
                        actualCards.Add(Instantiate(cardPrefabs[0], canvasParent)); // change index later
                        RectTransform c = actualCards[i].GetComponent<RectTransform>();
                        c.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Paper Shuriken":
                        actualCards.Add(Instantiate(cardPrefabs[1], canvasParent)); // change index later
                        RectTransform ca = actualCards[i].GetComponent<RectTransform>();
                        ca.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Mache Plateguard":
                        actualCards.Add(Instantiate(cardPrefabs[2], canvasParent)); // change index later
                        RectTransform cb = actualCards[i].GetComponent<RectTransform>();
                        cb.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Cardboard Spire":
                        actualCards.Add(Instantiate(cardPrefabs[3], canvasParent)); // change index later
                        RectTransform cc = actualCards[i].GetComponent<RectTransform>();
                        cc.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Box Slam":
                        actualCards.Add(Instantiate(cardPrefabs[4], canvasParent)); // change index later
                        RectTransform cd = actualCards[i].GetComponent<RectTransform>();
                        cd.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Origami Shredfall":
                        actualCards.Add(Instantiate(cardPrefabs[5], canvasParent)); // change index later
                        RectTransform ce = actualCards[i].GetComponent<RectTransform>();
                        ce.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Shredstorm Hurricane":
                        actualCards.Add(Instantiate(cardPrefabs[6], canvasParent)); // change index later
                        RectTransform cf = actualCards[i].GetComponent<RectTransform>();
                        cf.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Bottle Cap Barrage":
                        actualCards.Add(Instantiate(cardPrefabs[7], canvasParent)); // change index later
                        RectTransform cg = actualCards[i].GetComponent<RectTransform>();
                        cg.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Eco-Edge Sword":
                        actualCards.Add(Instantiate(cardPrefabs[8], canvasParent)); // change index later
                        RectTransform ch = actualCards[i].GetComponent<RectTransform>();
                        ch.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Bottle Blaster":
                        actualCards.Add(Instantiate(cardPrefabs[9], canvasParent)); // change index later
                        RectTransform ci = actualCards[i].GetComponent<RectTransform>();
                        ci.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Bottle Barrage":
                        actualCards.Add(Instantiate(cardPrefabs[10], canvasParent)); // change index later
                        RectTransform cj = actualCards[i].GetComponent<RectTransform>();
                        cj.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Recycled Polywall":
                        actualCards.Add(Instantiate(cardPrefabs[11], canvasParent)); // change index later
                        RectTransform ck = actualCards[i].GetComponent<RectTransform>();
                        ck.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Plastic Wave":
                        actualCards.Add(Instantiate(cardPrefabs[12], canvasParent)); // change index later
                        RectTransform cl = actualCards[i].GetComponent<RectTransform>();
                        cl.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Banana Splitter":
                        actualCards.Add(Instantiate(cardPrefabs[13], canvasParent)); // change index later
                        RectTransform cm = actualCards[i].GetComponent<RectTransform>();
                        cm.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Popping Wrapper":
                        actualCards.Add(Instantiate(cardPrefabs[14], canvasParent)); // change index later
                        RectTransform cn = actualCards[i].GetComponent<RectTransform>();
                        cn.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Croissant":
                        actualCards.Add(Instantiate(cardPrefabs[15], canvasParent)); // change index later
                        RectTransform co = actualCards[i].GetComponent<RectTransform>();
                        co.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "The Bonkstick":
                        actualCards.Add(Instantiate(cardPrefabs[16], canvasParent)); // change index later
                        RectTransform cp = actualCards[i].GetComponent<RectTransform>();
                        cp.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    case "Scavenge":
                        actualCards.Add(Instantiate(cardPrefabs[17], canvasParent)); // change index later
                        RectTransform cq = actualCards[i].GetComponent<RectTransform>();
                        cq.anchoredPosition = new Vector2(x * cardDistance.x, y * cardDistance.y + startingY);
                        break;
                    default:
                        break;
                }
                yield return new WaitForSeconds(.1f); // temp for seconds
            }
        }
        else
        {
            Debug.LogWarning("Card Names, Card Prefabs, or Card Name Count are Empty! Cards will not be shown.");
            noCards.SetActive(true);
        }
    }
}
