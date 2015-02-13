/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.StringReader;
import java.util.ArrayList;

import com.ib.controller.Profile.Allocation;
import com.ib.controller.Profile.Type;
import com.ib.controller.Types.Method;



public class AdvisorUtil {
	static ArrayList<Group> getGroups( String xml) {
		try {
			return getGroups_( xml);
		} catch (IOException e) {
			e.printStackTrace();
			return null;
		}
	}

	static ArrayList<Group> getGroups_( String xml) throws IOException {
		ArrayList<Group> list = new ArrayList<Group>();

		Group group = null;

		BufferedReader reader = new BufferedReader( new StringReader( xml) );
		String line;
		int state = 0; // 0=none; 1=list of groups; 2=reading group 3=listOfAccts
		while ( (line=reader.readLine()) != null) {
			line = line.trim();

			switch( state) {
				// top of file
				case 0:
					if (line.equals( "<ListOfGroups>")) {
						state = 1;
					}
					break;

				// reading groups
				case 1:
					if (line.equals( "<Group>")) {
						group = new Group();
						state = 2;
					}
					else if (line.equals( "</ListOfGroups>")) {
						state = 0;
					}
					else {
						err( line);
					}
					break;

				// reading group
				case 2:
					if (line.startsWith( "<name>") ) {
						group.name( getVal( line) );
					}
					else if (line.startsWith( "<defaultMethod>")) {
						group.defaultMethod( Method.valueOf( getVal( line) ) );
					}
					else if (line.startsWith( "<ListOfAccts")) {
						state = 3;
					}
					else if (line.equals( "</Group>")) {
						list.add( group);
						state = 1;
					}
					else {
						err( line);
					}
					break;

				// reading list of accts
				case 3:
					if (line.equals( "</ListOfAccts>")) {
						state = 2;
					}
					else {
						group.addAccount( getVal( line) );
					}
					break;
			}
		}

		return list;
	}

	static ArrayList<Profile> getProfiles( String xml) {
		try {
			return getProfiles_( xml);
		} catch (IOException e) {
			e.printStackTrace();
			return null;
		}
	}

	static ArrayList<Profile> getProfiles_( String xml) throws IOException {
		ArrayList<Profile> list = new ArrayList<Profile>();

		Profile profile = null;
		Allocation alloc = null;

		BufferedReader reader = new BufferedReader( new StringReader( xml) );
		String line;
		int state = 0; // 0=none; 1=list of groups; 2=reading group 3=listOfAllocations 4=allocation
		while ( (line=reader.readLine() ) != null) {
			line = line.trim();

			switch( state) {
				// top of file
				case 0:
					if (line.equals( "<ListOfAllocationProfiles>")) {
						state = 1;
					}
					break;

				// reading profiles
				case 1:
					if (line.equals( "<AllocationProfile>")) {
						profile = new Profile();
						state = 2;
					}
					else if (line.equals( "</ListOfAllocationProfiles>")) {
						state = 0;
					}
					else {
						err( line);
					}
					break;

				// reading Profile
				case 2:
					if (line.startsWith( "<name>") ) {
						profile.name( getVal( line) );
					}
					else if (line.startsWith( "<type>")) {
						int i = Integer.parseInt( getVal( line) );
						profile.type( Type.get( i) );
					}
					else if (line.startsWith( "<ListOfAllocations")) {
						state = 3;
					}
					else if (line.equals( "</AllocationProfile>")) {
						list.add( profile);
						state = 1;
					}
					else {
						err( line);
					}
					break;

				// reading list of allocations
				case 3:
					if (line.equals( "<Allocation>")) {
						alloc = new Allocation();
						state = 4;
					}
					else if (line.equals( "</ListOfAllocations>")) {
						state = 2;
					}
					else {
						err( line);
					}
					break;

				// reading Allocation
				case 4:
					if (line.startsWith( "<acct>") ) {
						alloc.account( getVal( line) );
					}
					else if (line.startsWith( "<amount>") ) {
						alloc.amount( getVal( line) );
					}
					else if (line.startsWith( "<posEff>") ) {
						// skip this
					}
					else if (line.equals( "</Allocation>") ) {
						profile.add( alloc);
						state = 3;
					}
					else {
						err( line);
					}
					break;
			}
		}

		return list;
	}

	static ArrayList<Alias> getAliases( String xml) {
		try {
			return getAliases_( xml);
		} catch (IOException e) {
			e.printStackTrace();
			return null;
		}
	}

	static ArrayList<Alias> getAliases_( String xml) throws IOException {
		ArrayList<Alias> list = new ArrayList<Alias>();

		Alias alias = null;

		BufferedReader reader = new BufferedReader( new StringReader( xml) );
		String line;
		int state = 0; // 0=none; 1=list of aliases; 2=reading alias
		while ( (line=reader.readLine() ) != null) {
			line = line.trim();

			switch( state) {
				// top of file
				case 0:
					if (line.equals( "<ListOfAccountAliases>")) {
						state = 1;
					}
					break;

				// reading aliases
				case 1:
					if (line.equals( "<AccountAlias>")) {
						alias = new Alias();
						state = 2;
					}
					else if (line.equals( "</ListOfAccountAliases>")) {
						state = 0;
					}
					else {
						err( line);
					}
					break;

				// reading Alias
				case 2:
					if (line.startsWith( "<account>") ) {
						alias.account( getVal( line) );
					}
					else if (line.startsWith( "<alias>")) {
						alias.alias( getVal( line) );
					}
					else if (line.equals( "</AccountAlias>")) {
						list.add( alias);
						state = 1;
					}
					else {
						err( line);
					}
					break;
			}
		}

		return list;
	}

	private static String getVal(String line) {
		int i1 = line.indexOf( '>');
		int i2 = line.indexOf( '<', 1);
		return line.substring( i1 + 1, i2);
	}

	private static void err(String line) {
		System.out.println( "error " + line);
	}


	public static void main(String[] args) {
		String str1 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<ListOfGroups>\n	<Group>\n		<name>Group 1</name>\n		<ListOfAccts varName=\"list\">\n			<String>DU109949</String>\n			<String>DU109950</String>\n			<String>DU110a156</String>\n			<String>DU110157</String>\n			<String>DU110158</String>\n		</ListOfAccts>\n		<defaultMethod>AvailableEquity</defaultMethod>\n	</Group>\n	<Group>\n		<name>Group 2</name>\n		<ListOfAccts varName=\"list\">\n			<String>DU109950</String>\n			<String>DU110156</String>\n			<String>DU110157</String>\n		</ListOfAccts>\n		<defaultMethod>AvailableEquity</defaultMethod>\n	</Group>\n</ListOfGroups>\n";
		ArrayList<Group> groups = getGroups( str1);

		String str2 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<ListOfAllocationProfiles>\n	<AllocationProfile>\n		<name>High Risk</name>\n		<type>1</type>\n		<ListOfAllocations varName=\"listOfAllocations\">\n			<Allocation>\n				<acct>DU110157</acct>\n				<amount>90.0</amount>\n				<posEff>O</posEff>\n			</Allocation>\n			<Allocation>\n				<acct>DU110158</acct>\n				<amount>10.0</amount>\n				<posEff>O</posEff>\n			</Allocation>\n		</ListOfAllocations>\n	</AllocationProfile>\n	<AllocationProfile>\n		<name>Profile</name>\n		<type>2</type>\n		<ListOfAllocations varName=\"listOfAllocations\">\n			<Allocation>\n				<acct>DU109949</acct>\n				<amount>1.0</amount>\n				<posEff>O</posEff>\n			</Allocation>\n		</ListOfAllocations>\n	</AllocationProfile>\n</ListOfAllocationProfiles>\n";
		ArrayList<Profile> profiles = getProfiles( str2);

		String str3 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<ListOfAccountAliases>\n	<AccountAlias>\n		<account>DF109948</account>\n		<alias>DF109948</alias>\n	</AccountAlias>\n	<AccountAlias>\n		<account>DU109949</account>\n		<alias>DU109949</alias>\n	</AccountAlias>\n	<AccountAlias>\n		<account>DU109950</account>\n		<alias>DU109950</alias>\n	</AccountAlias>\n	<AccountAlias>\n		<account>DU110156</account>\n		<alias>DU110156</alias>\n	</AccountAlias>\n	<AccountAlias>\n		<account>DU110157</account>\n		<alias>DU110157</alias>\n	</AccountAlias>\n	<AccountAlias>\n		<account>DU110158</account>\n		<alias>DU110158</alias>\n	</AccountAlias>\n</ListOfAccountAliases>\n\n";
		ArrayList<Alias> aliases = getAliases( str3);

		AdvisorUtil.err( aliases.toString() );
	}

	public static String getGroupsXml(ArrayList<Group> groups) {
		StringBuilder buf = new StringBuilder();
		buf.append( "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
		buf.append( "<ListOfGroups>\n");
		for( Group group : groups) {
			buf.append( "<Group>\n");
			buf.append( String.format( "<name>%s</name>\n", group.name() ) );
			buf.append( String.format( "<defaultMethod>%s</defaultMethod>\n", group.defaultMethod() ) );
			buf.append( "<ListOfAccts varName=\"list\"\n>");
			for( String acct : group.accounts() ) {
				buf.append( String.format( "<String>%s</String>\n", acct) );
			}
			buf.append( "</ListOfAccts>\n");
			buf.append( "</Group>\n");
		}
		buf.append( "</ListOfGroups>\n");
		return buf.toString();
	}

	public static String getProfilesXml(ArrayList<Profile> profiles) {
		StringBuilder buf = new StringBuilder();
		buf.append( "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
		buf.append( "<ListOfProfiles>\n");
		for( Profile profile : profiles) {
			buf.append( "<Profile>\n");
			buf.append( String.format( "<name>%s</name>\n", profile.name() ) );
			buf.append( String.format( "<type>%s</type>\n", profile.type().ordinal() ) );
			buf.append( "<ListOfAllocations varName=\"listOfAllocations\">\n");
			for( Allocation alloc : profile.allocations() ) {
				buf.append( "<Allocation>\n");
				buf.append( String.format( "<acct>%s</acct>\n", alloc.account() ) );
				buf.append( String.format( "<amount>%s</amount>\n", alloc.amount() ) );
				buf.append( "</Allocation>\n");
			}
			buf.append( "</ListOfAllocations>\n");
			buf.append( "</Profile>\n");
		}
		buf.append( "</ListOfProfiles>\n");
		return buf.toString();
	}
}
