using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yurowm.GameCore {
    public class SDSMask : MonoBehaviour {
        public string[] requirements = new string[0];

        void Awake() {
            if (!Content.main)
                Debug.LogError("Content manager is not found");
            foreach (string symbol in requirements) {
                if (!Content.main.SDSymbols.Contains(symbol)) {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
