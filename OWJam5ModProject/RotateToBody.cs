using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OWJam5ModProject
{
    public class RotateToBody : MonoBehaviour
    {
        [SerializeField] private string targetName = null;

        private Transform target = null;

        /**
         * Grab the target given the name
         */
        private void Start()
        {
            target = OWJam5ModProject.Instance.NewHorizons.GetPlanet(targetName).transform;
        }

        /**
         * Teleport to be facing the body
         */
        private void FixedUpdate()
        {
            //Figure out the way we want to face, cutting out any rotation not on the local y axis
            Vector3 toBody = target.position - transform.position;
            toBody = transform.InverseTransformDirection(toBody);
            toBody = new Vector3(toBody.x, 0, toBody.z);
            toBody = transform.TransformDirection(toBody);

            //Actually rotate
            float angle = Vector3.SignedAngle(transform.forward, toBody, transform.up);
            transform.Rotate(transform.up, angle, Space.World);
        }
    }
}
