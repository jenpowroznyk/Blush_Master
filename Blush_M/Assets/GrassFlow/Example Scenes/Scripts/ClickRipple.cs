using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrassFlow.Examples {
    public class ClickRipple : MonoBehaviour {

        public float rippleRate = 0.1f;
        public float contactOffset = 1f;
        public Collider grassCol;
        public float ripStrength;
        public float ripDecayRate;
        public float ripSpeed;
        public float ripRadius;

        float timer = 0;

        private void Update() {
            if (Input.GetMouseButton(0) && timer > rippleRate) {
                timer = 0;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (grassCol.Raycast(ray, out hit, 9999f)) {
                    GrassFlowRenderer.AddRipple(hit.point + hit.normal * contactOffset, ripStrength, ripDecayRate, ripSpeed, ripRadius, 0);
                }
            }

            timer += Time.deltaTime;
        }



    }
}