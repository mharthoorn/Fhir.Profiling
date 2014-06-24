﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Fhir.Profiling.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Fhir.Profiling.Tests
{
    [TestClass]
    public class JsonNavigatorTests
    {
        [TestMethod]
        public void TestConstruct()
        {
            var nav = buildNav();

            // At root;
            Assert.AreEqual(XPathNodeType.Root,nav.NodeType );
            Assert.IsFalse(nav.IsEmptyElement);
            Assert.AreEqual(String.Empty, nav.Name);
            Assert.AreEqual(String.Empty, nav.LocalName);
            Assert.AreEqual(String.Empty, nav.NamespaceURI);
            Assert.AreEqual(String.Empty, nav.Prefix);
            Assert.IsTrue(nav.Value.StartsWith("MRN2001-05-06Acme Healthcareusualurn:oid:1.2.36.146.595.217.0.112345Organization/1PeterJames" +
                "officialChalmersJimusualhttp://hl7.org/fhir/example-do-not-use#Patient.avatar#pic1Duck imageurn:example-do-not-use:pi3.141592653589793" +
                "http://hl7.org/fhir/v3/AdministrativeGenderMMale1974-12http://hl7.org/fhir/example-do-not-use#Patient.avatar#pic1Duck imagetruehome534 Erewhon St" +
                "PleasantVilleVic3999ASKUhttp://hl7.org/fhir/Profileiso-21090#nullFlavor3generated<div xmlns="));
        }


        [TestMethod]
        public void TestRootToChild()
        {
            var nav = buildNav();
            Assert.IsTrue(nav.MoveToFirstChild());
            Assert.AreEqual(XPathNodeType.Element, nav.NodeType);
            Assert.IsFalse(nav.IsEmptyElement);
            Assert.AreEqual("f:Patient", nav.Name);
            Assert.AreEqual("Patient", nav.LocalName);
            Assert.AreEqual(JsonXPathNavigator.FHIR_NS , nav.NamespaceURI);
            Assert.AreEqual(JsonXPathNavigator.FHIR_PREFIX, nav.Prefix);
        }

        [TestMethod]
        public void TestMoveToChild()
        {
            var nav = buildNav();
            Assert.IsTrue(nav.MoveToFirstChild());      // Patient
            Assert.IsTrue(nav.MoveToFirstChild());      // identifier
            Assert.IsTrue(nav.MoveToFirstChild());      // label
            
            Assert.AreEqual(XPathNodeType.Element, nav.NodeType);
            Assert.IsFalse(nav.IsEmptyElement);
            Assert.AreEqual("f:label", nav.Name);

            Assert.IsFalse(nav.MoveToFirstChild());      // No child
            Assert.IsTrue(nav.MoveToFirstAttribute());  // value attribute
            Assert.AreEqual(XPathNodeType.Attribute, nav.NodeType);
            Assert.AreEqual("MRN", nav.Value);

            Assert.IsFalse(nav.MoveToNextAttribute());       // no other attributes
            Assert.AreEqual(XPathNodeType.Attribute, nav.NodeType);  // should not have moved
            Assert.AreEqual("MRN", nav.Value);

            Assert.IsTrue(nav.MoveToParent());      // move up to label
            Assert.AreEqual(XPathNodeType.Element, nav.NodeType);
            Assert.AreEqual("f:label", nav.Name);

            Assert.IsTrue(nav.MoveToNext());        
            Assert.AreEqual(XPathNodeType.Element, nav.NodeType);
            Assert.AreEqual("f:period", nav.Name);

            Assert.IsTrue(nav.MoveToNext());        
            Assert.AreEqual(XPathNodeType.Element, nav.NodeType);
            Assert.AreEqual("f:assigner", nav.Name);
            Assert.IsTrue(nav.MoveToFirstChild());
            Assert.AreEqual("f:display", nav.Name);
            
            Assert.IsTrue(nav.MoveToParent());      // back to assigner

            Assert.IsTrue(nav.MoveToNext());        // move to use
            Assert.AreEqual(XPathNodeType.Element, nav.NodeType);
            Assert.AreEqual("f:use", nav.Name);

            Assert.IsTrue(nav.MoveToNext());        // move to system
            Assert.AreEqual(XPathNodeType.Element, nav.NodeType);
            Assert.AreEqual("f:system", nav.Name);

            Assert.IsTrue(nav.MoveToNext());        // move to value
            Assert.AreEqual(XPathNodeType.Element, nav.NodeType);
            Assert.AreEqual("f:value", nav.Name);

            Assert.IsFalse(nav.MoveToNext());
            Assert.IsTrue(nav.MoveToParent());  // identifier
            Assert.IsTrue(nav.MoveToParent());  // whole resource
            Assert.IsTrue(nav.MoveToParent()); // root
            Assert.IsFalse(nav.MoveToParent()); // root
        }


        [TestMethod]
        public void AttributeWithAppendix()
        {
            var nav = buildNav();
            Assert.IsTrue(nav.MoveToFirstChild());      // Patient

            Assert.IsTrue(nav.MoveToChild("contact", JsonXPathNavigator.FHIR_NS));
            Assert.IsTrue(nav.MoveToChild("name", JsonXPathNavigator.FHIR_NS));

            Assert.IsTrue(nav.MoveToFirstChild());  // family #1 - null
            Assert.AreEqual("f:family", nav.Name);
            Assert.IsFalse(nav.MoveToFirstAttribute()); // no value (null)
            Assert.IsTrue(nav.MoveToFirstChild());  // extension
            Assert.AreEqual("f:extension", nav.Name);
            nav.MoveToParent();

            Assert.IsTrue(nav.MoveToNext());        // family #2  - du
            Assert.AreEqual("f:family", nav.Name);
            
            Assert.IsTrue(nav.MoveToFirstAttribute()); // @value="du"
            Assert.AreEqual("du", nav.Value);
            Assert.AreEqual("value", nav.Name);
            Assert.IsTrue(nav.MoveToNextAttribute()); // @id="a2"
            Assert.AreEqual("a2", nav.Value);
            Assert.AreEqual("id", nav.Name);
            nav.MoveToParent();

            Assert.IsTrue(nav.MoveToNext());        // family #3  - null
            Assert.IsTrue(nav.MoveToNext());        // family #4  - Marché
            Assert.IsTrue(nav.MoveToNext());        // family #5  - null

            Assert.IsTrue(nav.MoveToNext());
            Assert.AreEqual("f:given", nav.Name);
        }

        [TestMethod]
        public void DivXhtmlNamespace()
        {
            var nav = buildNav();
            Assert.IsTrue(nav.MoveToFirstChild());      // Patient

            Assert.IsTrue(nav.MoveToChild("text", JsonXPathNavigator.FHIR_NS)); // Move to narrative
            Assert.IsTrue(nav.MoveToChild("div", JsonXPathNavigator.XHTML_NS)); // Move to narrative
            Assert.IsTrue(nav.Value.StartsWith("<div xmlns=\"http://www.w3.org/1999/xhtml\""));

        }

        [TestMethod]
        public void ForwardBackwards()
        {
            var nav = buildNav();
            Assert.IsTrue(nav.MoveToFirstChild());
            Assert.IsTrue(nav.MoveToFirstChild());      // nodeA
            Assert.IsTrue(nav.MoveToNext());    // nodeB[0]
            Assert.IsTrue(nav.MoveToNext());    // nodeB[1]
            Assert.IsTrue(nav.MoveToPrevious());
            Assert.IsTrue(nav.MoveToPrevious());

            Assert.IsFalse(nav.MoveToPrevious());  // one time too many
        }


        [TestMethod]
        public void CloneIsAtSamePosition()
        {
            Assert.IsNotNull(EqualityComparer<NavigatorState>.Default);

            var nav = buildNav();
            var nav2 = buildNav();
            Assert.IsTrue(nav.IsSamePosition(nav2));

            Assert.IsTrue(nav.MoveToFirstChild());
            Assert.IsTrue(nav.MoveToFirstChild()); // nodeA

            // Just checking, it should NOT be the same position
            Assert.IsFalse(nav.IsSamePosition(nav2));
            
            Assert.IsTrue(nav.MoveToNext()); // nodeB[0]
            Assert.IsTrue(nav.MoveToNext()); // nodeB[1]

            // Navigate the same with nav2
            Assert.IsTrue(nav2.MoveToFirstChild());
            Assert.IsTrue(nav2.MoveToFirstChild()); // nodeA
            Assert.IsTrue(nav2.MoveToNext()); // nodeB[0]
            Assert.IsTrue(nav2.MoveToNext()); // nodeB[1]
            
            // Now, they should have arrived at the same position
            Assert.IsTrue(nav.IsSamePosition(nav2));

            var nav3 = new JsonXPathNavigator(nav);
            Assert.IsTrue(nav.IsSamePosition(nav3));
            Assert.IsTrue(nav2.IsSamePosition(nav3));
        }


        [TestMethod]
        public void TestSelect()
        {
            var nav = buildNav();
            var mgr = new XmlNamespaceManager(nav.NameTable);
            mgr.AddNamespace("f", JsonXPathNavigator.FHIR_NS);
            
            var result = nav.Select("/f:Patient/f:telecom", mgr);
            Assert.AreEqual(2, result.Count);

            var text = nav.SelectSingleNode("/f:Patient/f:telecom[2]/f:use/@value", mgr);
            Assert.AreEqual("work", text.Value);

            text = nav.SelectSingleNode("/f:Patient/f:birthDate/@value", mgr);
            Assert.AreEqual("1974-12", text.Value);

            text = nav.SelectSingleNode("/f:Patient/f:birthDate/f:extension[@url='http://hl7.org/fhir/example-do-not-use#Patient.avatar']/f:valueResource/f:display", mgr);
            Assert.AreEqual("Duck image", text.Value);
        }


        [TestMethod]
        public void TestSelectContained()
        {
            var nav = buildNav();
            var mgr = new XmlNamespaceManager(nav.NameTable);
            mgr.AddNamespace("f", JsonXPathNavigator.FHIR_NS);

            var contained = nav.Select("/f:Patient/f:contained", mgr);
            Assert.IsNotNull(contained);
            Assert.AreEqual(2, contained.Count);

            var result = nav.SelectSingleNode("/f:Patient/f:contained/f:Organization/f:identifier/f:system/@value", mgr);
            Assert.AreEqual("urn:ietf:rfc:3986", result.Value);
        }

        private static JsonXPathNavigator buildNav()
        {
            //JsonReader r = new JsonTextReader(new StringReader(@"{ test : { nodeA: 5, nodeB: [4,'hoi',null], nodeC: 'text'} }"));
            //var nav = new JsonXPathNavigator(r);
            //return nav;

            Stream example = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Fhir.Profiling.Tests.TestPatient.json");

            return new JsonXPathNavigator(new JsonTextReader(new StreamReader(example)));
        }
    }
}
