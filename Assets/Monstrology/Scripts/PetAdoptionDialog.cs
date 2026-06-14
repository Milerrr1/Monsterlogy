using System;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class PetAdoptionDialog : MonoBehaviour
    {
        private CreatureCollectionManager collection;
        private GameObject root;
        private Image portrait;
        private Text speciesText;
        private InputField nameInput;
        private CreatureData pendingSpecies;
        private Action<bool> completed;

        public bool IsOpen { get { return root != null && root.activeSelf; } }

        public void Initialize(
            CreatureCollectionManager collectionManager,
            GameObject dialogRoot,
            Image portraitImage,
            Text speciesLabel,
            InputField customNameInput,
            Button acceptButton,
            Button declineButton)
        {
            collection = collectionManager;
            root = dialogRoot;
            portrait = portraitImage;
            speciesText = speciesLabel;
            nameInput = customNameInput;
            nameInput.characterLimit =
                CreatureCollectionManager.MaxCustomNameLength;

            acceptButton.onClick.AddListener(Accept);
            declineButton.onClick.AddListener(Decline);
            root.SetActive(false);
        }

        public void Show(CreatureData species, Action<bool> onCompleted)
        {
            if (species == null || collection == null || root == null)
            {
                return;
            }

            pendingSpecies = species;
            completed = onCompleted;
            speciesText.text =
                "Новый вид открыт и добавлен в питомцы:\n" +
                species.creatureName +
                "\n\nМожно сразу дать ему имя или оставить стандартное.";
            portrait.sprite = SpriteDatabase.Active.GetCreaturePortrait(species);
            portrait.color = SpriteDatabase.Active.HasCreatureArtwork(species)
                ? Color.white
                : Localization.RarityColor(species.rarity);
            nameInput.text = species.creatureName;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            nameInput.ActivateInputField();
        }

        public void Decline()
        {
            Finish(collection.GetPetBySpecies(
                pendingSpecies != null ? pendingSpecies.id : string.Empty) != null);
        }

        private void Accept()
        {
            CreatureInstance pet = pendingSpecies != null
                ? collection.AddPet(pendingSpecies.id)
                : null;
            bool saved = pet != null &&
                         collection.RenamePet(pet.uniqueId, nameInput.text);
            Finish(saved);
        }

        private void Finish(bool accepted)
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            pendingSpecies = null;
            Action<bool> callback = completed;
            completed = null;
            if (callback != null)
            {
                callback(accepted);
            }
        }
    }
}
