import pandas as pd
import matplotlib.pyplot as plt

# --- Chart 1: Nectar Obtained ---
print("Generating 'Nectar Obtained' box plot...")

# Load the data from the CSV files
try:
    agg_nectar = pd.read_csv('aggressive_nectar.csv')
    cons_nectar = pd.read_csv('conservative_nectar.csv')

    # The actual data is in the 'Value' column of the TensorBoard CSV
    agg_nectar_data = agg_nectar['Value']
    cons_nectar_data = cons_nectar['Value']

    # Combine the data into a list for plotting
    nectar_data_to_plot = [agg_nectar_data, cons_nectar_data]

    # Create the plot
    plt.figure(figsize=(8, 6)) # Set a good size for the chart
    plt.boxplot(nectar_data_to_plot, labels=['Aggressive', 'Conservative'])

    # Add titles and labels for clarity
    plt.title('Nectar Obtained per Episode (v4_GAMMA Run)', fontsize=16)
    plt.ylabel('Total Nectar Obtained', fontsize=12)
    plt.xlabel('Agent Strategy', fontsize=12)
    plt.grid(True, linestyle='--', alpha=0.6) # Add a grid for readability

    # Save the figure as a high-quality PNG file
    plt.savefig('Chart_5_Nectar_Obtained.png', dpi=300)
    print("'Chart_5_Nectar_Obtained.png' saved successfully.")

except FileNotFoundError as e:
    print(f"Error creating Nectar plot: {e}. Make sure CSV files are in the same folder and named correctly.")


# --- Chart 2: Energy Efficiency ---
print("\nGenerating 'Energy Efficiency' box plot...")

# Load the data from the CSV files
try:
    agg_eff = pd.read_csv('aggressive_efficiency.csv')
    cons_eff = pd.read_csv('conservative_efficiency.csv')

    # The actual data is in the 'Value' column
    agg_eff_data = agg_eff['Value']
    cons_eff_data = cons_eff['Value']

    # Combine the data for plotting
    efficiency_data_to_plot = [agg_eff_data, cons_eff_data]

    # Create the plot
    plt.figure(figsize=(8, 6))
    plt.boxplot(efficiency_data_to_plot, labels=['Aggressive', 'Conservative'])

    # Add titles and labels
    plt.title('Energy Efficiency per Episode (v4_GAMMA Run)', fontsize=16)
    plt.ylabel('Energy Efficiency (Nectar / Steps)', fontsize=12)
    plt.xlabel('Agent Strategy', fontsize=12)
    plt.grid(True, linestyle='--', alpha=0.6)

    # Save the figure
    plt.savefig('Chart_6_Energy_Efficiency.png', dpi=300)
    print("'Chart_6_Energy_Efficiency.png' saved successfully.")

except FileNotFoundError as e:
    print(f"Error creating Efficiency plot: {e}. Make sure CSV files are in the same folder and named correctly.")


# --- Chart 3: Survival Time ---
print("\nGenerating 'Survival Time' box plot...")

# Load the data from the CSV files
try:
    agg_survival = pd.read_csv('aggressive_survival.csv')
    cons_survival = pd.read_csv('conservative_survival.csv')

    # The actual data is in the 'Value' column
    agg_survival_data = agg_survival['Value']
    cons_survival_data = cons_survival['Value']

    # Combine the data for plotting
    survival_data_to_plot = [agg_survival_data, cons_survival_data]

    # Create the plot
    plt.figure(figsize=(8, 6))
    plt.boxplot(survival_data_to_plot, labels=['Aggressive', 'Conservative'])

    # Add titles and labels
    plt.title('Survival Time per Episode (v4_GAMMA Run)', fontsize=16)
    plt.ylabel('Steps Survived', fontsize=12)
    plt.xlabel('Agent Strategy', fontsize=12)
    plt.grid(True, linestyle='--', alpha=0.6)

    # Save the figure
    plt.savefig('Chart_7_Survival_Time.png', dpi=300)
    print("'Chart_7_Survival_Time.png' saved successfully.")

except FileNotFoundError as e:
    print(f"Error creating Survival Time plot: {e}. Make sure CSV files are in the same folder and named correctly.")


print("\nAll plots generated.")

# If you want to see the plots pop up after saving, uncomment the line below
# plt.show()