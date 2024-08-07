This shader just reads and displays TEXCOORD3.  
(With a `pow()` to make it more visually obvious).

I.e. `return pow(TEXCOORD3.xy * 0.8, 3);`