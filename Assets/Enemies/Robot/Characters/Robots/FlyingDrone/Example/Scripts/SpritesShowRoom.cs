// Copyright (c) 2025 Erochin Semyon. All rights reserved
// Licensed under the Standard Unity Asset Store EULA License for Unity users

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voo.ItemsShowRoom
{
    public class SpritesShowRoom : MonoBehaviour
    {
        public Image Template;
        public Transform RootSpawn;
        public Sprite[] Sprites;

        private readonly List<GameObject> _items = new ();

        public void Awake()
        {
            if (Sprites == null)
                return;

            foreach (var sprite in Sprites)
            {
                var item = GameObject.Instantiate(Template.gameObject, RootSpawn);
                item.GetComponent<Image>().sprite = sprite;
                _items.Add(item);
            }
            Template.gameObject.SetActive(false);
        }
    }
}
