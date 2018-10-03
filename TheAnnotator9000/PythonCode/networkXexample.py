#-------------------------------------------------------------------------------
# Name:        module1
# Purpose:
#
# Author:      edkaz
#
# Created:     12/09/2018
# Copyright:   (c) edkaz 2018
# Licence:     <your licence>
#-------------------------------------------------------------------------------

# libraries
import pandas as pd
import numpy as np
import networkx as nx
import matplotlib.pyplot as plt
import json
import pydot
from networkx.drawing.nx_pydot import graphviz_layout

def main():
    # Build a dataframe with 4 connections
    df = pd.DataFrame({ 'from':['A', 'B', 'C','A'], 'to':['D', 'A', 'E','C']})

    # Build your graph
    G=nx.from_pandas_edgelist(df, 'from', 'to')
    pos = graphviz_layout(G, prog='twopi', args='')

    # Plot it
    nx.draw(G, pos, with_labels=True, node_color="skyblue", alpha=0.5, linewidths=40)
    plt.show()

    Jsonpath = 'C:/Users/edkaz/AppData/Local/Packages/f492410c-3390-4254-bf50-83137f1388b2_dv867kpbcjyry/LocalState/JSONs/testJSON.json'


##    with open(Jsonpath) as data_file:
##        data = json.load(data_file)
##        print(data)
##
##        dataframe = pd.DataFrame(data)
##
##        D = nx.Graph(data)
##
##        # Build your graph
##        G=nx.from_pandas_edgelist(dataframe)
##
##        # Plot it
##        nx.draw(G, with_labels=True, node_color="skyblue", alpha=0.5, linewidths=40)
##        plt.show()

if __name__ == '__main__':
    main()
