import json
import os
import matplotlib
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import seaborn as sns

matplotlib.use('agg')

def plot_and_save(filePath):
    plt.savefig(filePath)
    plt.close()

def overlay_game_image(ax, x_size, y_size, imagefile='zombiemap.png'):
    # get the image as an array so we can plot it 
    import matplotlib.image as mpimg 
    map_img = mpimg.imread(imagefile) 
    # map_img = np.rot90(map_img, 2) # rotate image by 90 deg
    # map_img = np.fliplr(map_img) # flip image horizontally

    ax.imshow(map_img,
          aspect = ax.get_aspect(),
          origin = 'lower',
          extent = (0, x_size, y_size+4, 0),
          zorder = 1) #put the map under the heatmap

"""
    createHeatmap1 - creates naive heatmap

    data (dictionary): heatmap data
    filePath (string): path to save heatmap
""" 
def createHeatmap1(data, filePath):
    print("Heatmap 1: Naive heatmap")

    # Concatenate all positional data across episodes
    pos_x, pos_y = [], []
    for eps in data['episodes']:
        pos_x.append(eps['pos_x'])
        pos_y.append(eps['pos_y'])

    # Bin values
    pos_x, pos_y = np.concatenate(pos_x), np.concatenate(pos_y)
    x_size, y_size = int(max(pos_x) - min(pos_x)), int(max(pos_y) - min(pos_y))
    H, _, _ = np.histogram2d(pos_x, pos_y, bins=[x_size, y_size])
    
    # Plot heatmap and save figures
    ax = sns.heatmap(np.log(H.T+1), cmap="BuPu", yticklabels=20)
    plot_and_save(filePath)


"""
    createHeatmap2 - creates heatmap based on percentage highest/lowest gamescore

    data (dictionary): heatmap data
    percentile (decimal): percentage of highest/lowest gamescore to filter by
    highest (boolean): True = filter by top percentile, False = filter by bottom percentile
    filePath (string): path to save heatmap
"""
def createHeatmap2(data, percentile, highest, filePath):
    assert 0 < percentile <= 1
    
    # Convert data into dataframe
    df = pd.DataFrame.from_dict(data['episodes'])
    df.head()

    # Sort episodes by highest/lowest gamescore (i.e., reward)    
    sorted_df = df.sort_values(by=['reward'], ascending=not highest)

    # Filter by percentile
    filtered_df = sorted_df.iloc[:int(len(sorted_df)*percentile), :]

    # Concatenate all positional data across filtered episodes
    pos_x, pos_y = np.concatenate(filtered_df['pos_x'].to_numpy()), np.concatenate(filtered_df['pos_y'].to_numpy())
    x_size, y_size = int(max(pos_x) - min(pos_x)), int(max(pos_y) - min(pos_y))

    # Bin values
    H, _, _ = np.histogram2d(pos_x, pos_y, bins=[x_size, y_size])

    # Plot heatmap and save figures
    sns.heatmap(np.log(H.T+1), yticklabels=20)

    if highest:
        print("Heatmap 2: Top %f%% highest gamescore episodes" % (percentile*100))
        plot_and_save(filePath)
    else:
        print("Heatmap 2: Bottom %f%% lowest gamescore episodes" % (percentile*100))
        plot_and_save(filePath)


"""
    createHeatmap3 - creates heatmap based on percentage longest/shortest duration

    data (dictionary): heatmap data
    percentile (decimal): percentage of highest/lowest gamescore to filter by
    longest (boolean): True = filter by top percentile, False = filter by bottom percentile
    filePath (string): path to save heatmap
"""
def createHeatmap3(data, percentile, longest, filePath):
    assert 0 < percentile <= 1
    
    # Convert data into dataframe
    df = pd.DataFrame.from_dict(data['episodes'])
    df.head()

    # Get percentile episodes that are longest/shortest  
    sorted_df = df.sort_values(by=['step_num'], ascending=not longest)

    # Filter by percentile
    filtered_df = sorted_df.iloc[:int(len(sorted_df)*percentile), :]

    # Concatenate all positional data across filtered episodes
    pos_x, pos_y = np.concatenate(filtered_df['pos_x'].to_numpy()), np.concatenate(filtered_df['pos_y'].to_numpy())
    x_size, y_size = int(max(pos_x) - min(pos_x)), int(max(pos_y) - min(pos_y))

    # Bin values
    H, _, _ = np.histogram2d(pos_x, pos_y, bins=[x_size, y_size])

    # Plot heatmap and save figures
    sns.heatmap(np.log(H.T+1), yticklabels=20)

    if longest:
        print("Heatmap 3: Top %f%% longest duration episodes" % (percentile*100))
        plot_and_save(filePath)
    else:
        print("Heatmap 3: Top %f%% shortest duration episodes" % (percentile*100))
        plot_and_save(filePath)


"""
    createHeatmap4 - creates last position heatmap

    data (dictionary): heatmap data
    filePath (string): path to save heatmap
""" 
def createHeatmap4(data, filePath):
    print("Heatmap 4: Last position of every episode")
    
    # Get last position of every episode
    pos_x, pos_y = [], []
    for eps in data['episodes']:
        pos_x.append(eps['pos_x'][-1])
        pos_y.append(eps['pos_y'][-1])
    pos_x, pos_y = np.array(pos_x), np.array(pos_y)

    # Bin values
    pos_x, pos_y = np.array(pos_x), np.array(pos_y)
    x_size, y_size = int(max(pos_x) - min(pos_x)), int(max(pos_y) - min(pos_y))
    H, _, _ = np.histogram2d(pos_x, pos_y, bins=[x_size, y_size])
    
    # Plot heatmap and save figures
    sns.heatmap(np.log(H.T+1), cmap="BuPu", yticklabels=20)
    plot_and_save(filePath)


"""
    createHeatmap5 - can specify a minimum reward they want

    data (dictionary): heatmap data
    minReward (float): minimum reward
    filePath (string): path to save heatmap
""" 
def createHeatmap5(data, minReward, filePath):
    print("Heatmap 5: User can specify a specific minimum reward they want")
    
    # Convert data into dataframe
    df = pd.DataFrame.from_dict(data['episodes'])
    df.head()

    # Get episodes that have at least the minimum reward
    filtered_df = df[ df['reward'] > minReward]
    filtered_df.head()

    # If there are no results return early
    if len(filtered_df) <= 1:
        print("There are no results")
        return

    # Concatenate all positional data across filtered episodes
    pos_x, pos_y = np.concatenate(filtered_df['pos_x'].to_numpy()), np.concatenate(filtered_df['pos_y'].to_numpy())
    x_size, y_size = int(max(pos_x) - min(pos_x)), int(max(pos_y) - min(pos_y))

    # Bin values
    H, _, _ = np.histogram2d(pos_x, pos_y, bins=[x_size, y_size])

    # Plot heatmap and save figures
    sns.heatmap(np.log(H.T+1), yticklabels=20)
    plot_and_save(filePath)


"""
    createHeatmap6 - can specify a minimum step count they want

    data (dictionary): heatmap data
    minSteps (int): minimum steps
    filePath (string): path to save heatmap
""" 
def createHeatmap6(data, minSteps, filePath):
    print("Heatmap 6: User can specify a specific minimum number of steps they want")
    
    # Convert data into dataframe
    df = pd.DataFrame.from_dict(data['episodes'])
    df.head()

    # Get episodes that have at least the minimum reward
    filtered_df = df[ df['step_num'] > minSteps ]
    filtered_df.head()

    # If there are no results return early
    if len(filtered_df) <= 1:
        print("There are no results")
        return

    # Concatenate all positional data across filtered episodes
    pos_x, pos_y = np.concatenate(filtered_df['pos_x'].to_numpy()), np.concatenate(filtered_df['pos_y'].to_numpy())
    x_size, y_size = int(max(pos_x) - min(pos_x)), int(max(pos_y) - min(pos_y))

    # Bin values
    H, _, _ = np.histogram2d(pos_x, pos_y, bins=[x_size, y_size])

    # Plot heatmap and save figures
    sns.heatmap(np.log(H.T+1), yticklabels=20)
    plot_and_save(filePath)
