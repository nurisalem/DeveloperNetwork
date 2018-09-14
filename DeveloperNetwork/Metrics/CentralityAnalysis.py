# Import all necessary modules
import networkx as nx
import csv
import matplotlib.pyplot as plt
import os

# Prepare graph
G = nx.Graph()

# Add nodes
with open('CentralityNodes.txt', 'r') as f:
    for node in f.readlines():
        G.add_node(node.strip())

# Add edges
with open('CentralityEdges.txt', 'r') as f:
    for edge in f.readlines():
        edgeData = edge.split(',')
        G.add_edge(edgeData[0].strip(), edgeData[1].strip())

# Analyze graph
closeness = dict(nx.closeness_centrality(G))
betweenness = dict(nx.betweenness_centrality(G))
centrality = dict(nx.degree_centrality(G))

# Combine results of analysis
combined = {}
for key in closeness:
    single = {
            'Author': key,
            'Closeness': closeness[key],
            'Betweenness': betweenness[key],
            'Centrality': centrality[key]
            }
    
    combined[key] = single

# Output combined results to a CSV
with open('CentralityResults.csv', 'w', newline='') as f:
    w = csv.DictWriter(f, ['Author','Closeness','Betweenness','Centrality'])
    w.writeheader()
    
    for key in combined:
        w.writerow(combined[key])
        
# Output graph to PNG
graphFigure = plt.figure(5,figsize=(30,30))
nx.draw(G, with_labels=True, node_color='orange', node_size=4000, edge_color='black', linewidths=2, font_size=20)
graphFigure.savefig("CentralityGraph.png")

# Clean up
#os.remove("CentralityNodes.txt")
#os.remove("CentralityEdges.txt")