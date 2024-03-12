= Petite modif rapport

== Intro

Mettre en premier colaboration à distance et ajouter science (experience avec cobaye)

== Existant

Leap Motion a l'air d'être vraiment l'outil adapté à notre utilisation, tout seul, il remplace la kinect et baracuda.

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


L'utilisation combiné de la caméra infrarouge et couleur pose problème, car les deux caméras on une position différente, même une orientation légèrement divergente. La capture d'image n'est pas non plus synchronisé, signifiant qu'il est difficil, lorsque la main est en mouvement, d'obtenir deux frames capturant le même instant. Je soupsonne que la capture est peut-être synchronisée et qu'il faut regardé dans l'API Kinect s'il n'y a pas un timestamp associé au frame. De plus, une calibration principallement automatique (ne requierant pas d'action particulière de l'utilisateur) pourrait déterminer la position relative des deux caméra. Si ces deux difficulté peuvent être résolues, alors le flux vidéo combiné pourrait être le moyen le plus efficace, optimisé et résiliant pour effectuer la captation des mains.

Par ailleurs, il y a un troisième défit, sur comment combiner les deux informations, les canaux rouge, vert et bleu, et le canal en niveau de gris infrarouge. Ma première idée etait de multiplier les canaux couleurs par l'intensité infrarouge pour faire ressortir des contrasts normalement absent sur l'image couleur. Notament car sur l'infrarouge, il y a souvent un trés bon contrast entre la main et le fond, puisque l'éclairage est actif depuis la Kinect et fais ressortir la main en plus clair. Cependant, cette méthode, tant que les deux images ne peuvent pas être parfaitement superposée, introduit des doubles contours qui semblent plus pertuber la capatation des mains qu'autre chose.

La deuxième aproche était de ne pas prendre en compte le contrast lumineux de la caméra couleur, mais uniquement d'utiliser la teinte et la saturation pour l'ajouter à l'image infrarouge, que l'information de couleur ne devrait pas introduire de contour. Il a fallut implémenter dans un shader deux fonctions `rgb_to_hsv` et `hsv_to_rgb`. Cependant ça ne donne pas non plus de bon résultat. Il semble qu'un alignement parfait des deux images est véritablement une nécéssité. Deplus, pour l'alignement, il faut choisir la profondeur sur laquelle on aligne l'image, donc si la main est orientée de sorte que certaine parties sont plus éloignées et d'autres plus proche, la superposition des deux images ne peut pas être parfaite sur l'intégralité de la main. Il est possible grâce au donné de la Kinect de supposé une inclinaison globale de la main et d'appliquer un alignement progressif dans la direction où l'éloignement varie.

La dernière piste est simplement de faire deux analyses à chaque fois, avec la couleur et avec l'infrarouge, et de ne garder le résultat de celle qui obtient le meilleur score. On a de la résilience mais une utilisation de ressources doublées, avec peut-être, sur une machine peu puissante, une perte de fluidité. 

HandPose peut tracker les deux mains, en lançant l'analyse une première fois, en récupérant la parité de la main, puis en lançant une deuxième fois l'analyse, mais cette fois-ci en masquant la main détecté lors de la première analyse.
