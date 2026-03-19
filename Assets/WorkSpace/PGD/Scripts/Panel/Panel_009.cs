using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PGD
{
    public class Panel_009 : MonoBehaviour
    {
        [SerializeField] private SCC scc;

        [SerializeField] private TMP_InputField inputField_flight;
        [SerializeField] private TMP_InputField inputField_pou;
        [SerializeField] private TMP_InputField inputField_awb;
        [SerializeField] private TMP_InputField inputField_scc;
        [SerializeField] private TMP_InputField inputField_repeatCount;

        [SerializeField] private Transform schedulingScrollViewPos;
        [SerializeField] private GameObject prefab_schedulingCell;

        [SerializeField] private TMP_Text text_filght;
        [SerializeField] private Sprite sprite_sccBG;

        [SerializeField] GameObject[] pouList;
        [SerializeField] GameObject[] awbList;
        [SerializeField] List<int> awbCountList;
        [SerializeField] Image[] sccList;

        private int pouCount = -1;
        private int sccCount = -1;

        private void ResetMakeDB()
        {
            inputField_flight.text = null;
            inputField_pou.text = null;
            inputField_awb.text = null;
            inputField_scc.text = null;
            inputField_repeatCount.text = null;

            for (int i = 0; i < pouList.Length; i++)
            {
                pouList[i].SetActive(false);
            }
            for (int i = 0; i < awbList.Length; i++)
            {
                awbList[i].SetActive(false);
            }
            for (int i = 0; i < sccList.Length; i++)
            {
                sccList[i].gameObject.SetActive(false);
            }

            awbCountList.Clear();
            pouCount = -1;
            sccCount = -1;
        }

        public void OnClickPOUPlusBtn()
        {
            if (pouCount >= pouList.Length - 1)
                return;

            pouCount++;
            pouList[pouCount].transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = inputField_pou.text;
            pouList[pouCount].SetActive(true);
            awbList[pouCount].SetActive(true);
            awbList[pouCount].transform.GetChild(0).GetComponent<TMP_Text>().text = inputField_awb.text;
            awbCountList.Add(int.Parse(inputField_awb.text));
        }

        public void OnClickPOUMinusBtn()
        {
            if (pouCount < 1)
                return;

            pouList[pouCount].SetActive(false);
            awbList[pouCount].SetActive(false);
            awbCountList.RemoveAt(awbCountList.Count - 1);
            pouCount--;
        }
        public void OnClickSCCPlusBtn()
        {
            sccCount++;
            if (scc.SCCMap.ContainsKey(inputField_scc.text))
            {
                sccList[sccCount].sprite = scc.SCCMap[inputField_scc.text];
                sccList[sccCount].transform.GetChild(0).GetComponent<TMP_Text>().text = inputField_scc.text;
                sccList[sccCount].transform.GetChild(0).gameObject.SetActive(false);
            }
            else
            {
                sccList[sccCount].sprite = sprite_sccBG;
                sccList[sccCount].transform.GetChild(0).GetComponent<TMP_Text>().text = inputField_scc.text;
                sccList[sccCount].transform.GetChild(0).gameObject.SetActive(true);
            }

            sccList[sccCount].gameObject.SetActive(true);
        }

        public void OnClickSCCMinusBtn()
        {
            if (sccCount < 1)
                return;

            sccList[sccCount].gameObject.SetActive(false);
            sccCount--;
        }

        public void OnClickMakeDB()
        {
            ExportCSV();
        }

        private void ExportCSV()
        {
            string path = Application.streamingAssetsPath + "/" + inputField_flight.text.ToString() + "_" + System.DateTime.Now.ToString("yyyyMMdd") + ".csv";
            print("path " + path);
            List<string[]> cargoData = new List<string[]>();
            string[] headers = { "ID", "Flight", "POU", "SCC", "WV", "X", "Y", "Z", "W", "SQV" };
            string[] tempCargoData = new string[headers.Length];

            for (int i = 0; i < headers.Length; i++)
            {
                tempCargoData[i] = headers[i];
            }

            cargoData.Add(tempCargoData);

            int awbLenght = awbCountList.Sum();
            int[] volume = { 1, 1, 1 };

            for (int i = 0; i < awbLenght; i++)
            {
                tempCargoData = new string[headers.Length];
                tempCargoData[0] = (i + 1).ToString();
                tempCargoData[1] = inputField_flight.text;
                int random = 0;

                bool isPass = false;

                while (!isPass)
                {
                    random = Random.Range(0, (pouCount + 1));
                    if (awbCountList[random] > 0)
                    {
                        awbCountList[random]--;
                        tempCargoData[2] = pouList[random].transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text;
                        isPass = true;
                    }
                }

                List<string> randomSccList = new List<string>();

                for (int j = 0; j <= sccCount; j++)
                {
                    randomSccList.Add(sccList[j].transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text);
                }

                randomSccList = ShuffleList(randomSccList);

                random = Random.Range(0, randomSccList.Count);
                
                for (int j = 0; j <= random; j++)
                {
                    tempCargoData[3] += randomSccList[j] + "+";
                }

                tempCargoData[3] = tempCargoData[3].TrimEnd('+');

                int WV = Random.Range(200, 231);

                tempCargoData[4] = WV.ToString();

                int SQV = (int)(WV * 1.2f);
                tempCargoData[9] = SQV.ToString();
                List<int> factorList = new List<int>();

                for (int j = 2; j < SQV + 1; j++)
                {
                    while (SQV % j == 0)
                    {
                        factorList.Add(j);
                        SQV /= j;
                    }
                }

                if (factorList.Count < 3)
                {
                    for (int n = 0; n < 3 - factorList.Count; n++)
                    {
                        factorList.Add(1);
                    }
                }

                factorList = ShuffleList(factorList);

                int startValue = 0;

                for (int j = 0; j < 3; j++)
                {
                    if (j == 2)
                    {
                        for (int k = startValue; k < factorList.Count; k++)
                        {
                            volume[j] *= factorList[k];
                        }
                    }
                    else
                    {
                        random = Random.Range(startValue, factorList.Count - (2-j) + 1);

                        for (int k = startValue; k < random; k++)
                        {
                            volume[j] *= factorList[k];
                        }

                        startValue = random;
                    }
                }

                for (int m = 0; m < volume.Length; m++)
                {
                    tempCargoData[5 + m] = volume[m].ToString();
                    volume[m] = 1;
                }

                tempCargoData[8] = Random.Range(100, 121).ToString();

                cargoData.Add(tempCargoData);
            }

            string[][] output = new string[cargoData.Count][];

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = cargoData[i];
            }

            int length = output.GetLength(0);
            string delimiter = ",";

            StringBuilder sb = new StringBuilder();

            for (int index = 0; index < length; index++)
            {
                sb.AppendLine(string.Join(delimiter, output[index]));
            }

            StreamWriter outStream = File.CreateText(path);
            outStream.WriteLine(sb);
            outStream.Close();

            GameObject cell = Instantiate(prefab_schedulingCell, schedulingScrollViewPos);
            cell.GetComponent<SchedulingCell>().text_name.text = tempCargoData[1];
            cell.GetComponent<SchedulingCell>().text_count.text = "[" + awbLenght.ToString() + "]";

            ResetMakeDB();
        }

        private List<T> ShuffleList<T>(List<T> list)
        {
            int random1, random2;
            T temp;

            for (int i = 0; i < list.Count; ++i)
            {
                random1 = Random.Range(0, list.Count);
                random2 = Random.Range(0, list.Count);

                temp = list[random1];
                list[random1] = list[random2];
                list[random2] = temp;
            }

            return list;
        }
        public void OnClickStartBtn()
        {
            SimulationModeTaskManager.Instance.Igniter();
            this.gameObject.SetActive(false);
        }

        public void OnClickResultOnlyBtn()
        {

        }
    }
}

