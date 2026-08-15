using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Question Library", fileName = "QuestionLibrary")]
public class QuestionLibrary : ScriptableObject
{
    public List<QuestionData> questions = new List<QuestionData>();
}
