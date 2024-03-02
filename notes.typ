= Advanced Tracking

Est-il possible d'obtenir un alignement parfait de l'image couleur et infrarouge ?

Peut-être, car c'est la caméra couleur que n'est pas le point origine, mais c'est la caméra infrarouge.
Il faut donc appliquer le décalage sur la couleur, pas l'infrarouge

[ ] Analyser l'infrarouge et la couleur, ne garder que le meilleur score

[X] Use min-max on infrared to boost contrast dynamically
[X] Do not start new analysis before the previous one is finished

[/] Keep track of was is being correctly tracked and interpolate
[/] Use kinect dept and infra red to power up the baracuda input
[ ] Add softening layer on kinect tracking, like considering inertia