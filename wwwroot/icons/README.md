# Icônes PWA pour CTSAR Booking

## Icônes requises

Pour que l'application fonctionne correctement en tant que PWA (Progressive Web App), vous devez créer les icônes suivantes dans ce dossier :

- **icon-192x192.png** : 192x192 pixels (minimum requis pour Android)
- **icon-512x512.png** : 512x512 pixels (recommandé pour Android)

## Comment créer les icônes

### Option 1 : Utiliser un générateur en ligne

1. Allez sur un générateur d'icônes PWA comme :
   - https://realfavicongenerator.net/
   - https://www.pwabuilder.com/imageGenerator
   - https://progressier.com/pwa-icons-and-ios-splash-screen-generator

2. Téléchargez votre logo/icône source (idéalement un carré de 1024x1024 pixels)

3. Générez les icônes et téléchargez-les

4. Placez les fichiers générés dans ce dossier

### Option 2 : Utiliser un logiciel de design

Si vous utilisez Photoshop, GIMP, Figma, etc. :

1. Créez une image carrée avec votre logo
2. Exportez en 192x192 pixels → `icon-192x192.png`
3. Exportez en 512x512 pixels → `icon-512x512.png`

## Recommandations de design

- **Zone de sécurité** : Gardez les éléments importants au centre (zone de 80% du carré)
- **Fond** : Ajoutez un fond de couleur (recommandé : #3473BE pour correspondre au thème)
- **Simplicité** : L'icône doit être reconnaissable même en petit format
- **Contraste** : Assurez-vous que le logo est bien visible sur différents fonds

## Test

Pour tester votre icône PWA :

### Sur Android/Chrome
1. Ouvrez le site sur Chrome mobile
2. Menu → "Ajouter à l'écran d'accueil"
3. L'icône devrait apparaître

### Sur iOS/Safari
1. Ouvrez le site sur Safari mobile
2. Bouton Partager → "Sur l'écran d'accueil"
3. L'icône devrait apparaître

## Icône actuelle

Actuellement, le fichier `favicon.png` à la racine de wwwroot est utilisé comme fallback.
Vous pouvez l'utiliser comme base pour créer vos icônes PWA.
