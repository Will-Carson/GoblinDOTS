
using UnityEngine;


public static class ClayxelsPrefs {
    public static void apply(){
    	// how many solids can you have in a container
        // affects video memory
        Clayxels.ClayContainer.setMaxSolids(512);// 64, 128, 256, 512, 1024, 4096, 16384

        // max size of the container work area
        // only has effect in the UI, no performance change
        Clayxels.ClayContainer.setMaxChunks(3, 3, 3);// don't exceed 4,4,4 to avoid filling up video memory
        
        // how many solids you can have in a single voxel
        // lower values means faster and less video memory used
        Clayxels.ClayContainer.setMaxSolidsPerVoxel(128);// 32, 64, 128, 256, 512, 1024, 2048

        // skip a certain amount of frames before updating
        // useful to make large containers go faster in game
        Clayxels.ClayContainer.setUpdateFrameSkip(0);

        // update a certain amount of chunks per frame
        // small numbers will reduce the workload on GPU per frame
        Clayxels.ClayContainer.setChunksUpdatePerFrame(64); // default is 64 to allow all chunks in a 4,4,4 container to update in one frame
    }
}
