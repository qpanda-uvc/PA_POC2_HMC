using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PGD
{
    public class Panel_006 : MonoBehaviour
    {
        [SerializeField] private Image slider_loading;

        void Start()
        {
            StartCoroutine(LoadScene("SimulationModeScene1"));
        }

        private IEnumerator LoadScene(string sceenName)
        {
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceenName);
            asyncOperation.allowSceneActivation = false;

            while (asyncOperation.progress < 0.9f)
            {
                yield return null;
                slider_loading.fillAmount = asyncOperation.progress;
            }

            yield return new WaitForSeconds(2);
            asyncOperation.allowSceneActivation = true;
        }
    }
}


